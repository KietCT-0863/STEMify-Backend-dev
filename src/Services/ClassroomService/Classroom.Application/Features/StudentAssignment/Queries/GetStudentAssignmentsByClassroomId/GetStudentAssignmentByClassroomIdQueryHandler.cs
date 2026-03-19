using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Specifications.CourseEnrollments;
using Classroom.Application.Specifications.CurriculumEnrollments;
using Classroom.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentsByClassroomId
{
    public class GetStudentAssignmentByClassroomIdQueryHandler : IRequestHandler<GetStudentAssignmentByClassroomIdQuery, GrpcPagedStudentAssignmentsResponse>
    {
        private readonly ILogger<GetStudentAssignmentByClassroomIdQueryHandler> _logger;
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;
        private readonly IGrpcUserClient _grpcUserClient;

        public GetStudentAssignmentByClassroomIdQueryHandler(
            ILogger<GetStudentAssignmentByClassroomIdQueryHandler> logger,
            IClassroomUnitOfWork unitOfWork,
            IGrpcAssignmentClient grpcAssignmentClient,
            IGrpcUserClient grpcUserClient)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcAssignmentClient = grpcAssignmentClient;
            _grpcUserClient = grpcUserClient;
        }

        public async Task<GrpcPagedStudentAssignmentsResponse> Handle(GetStudentAssignmentByClassroomIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetCourseEnrollmentByClassroomIdSpecification(request.ClassroomId);
            var curriculumEnrollments = await _unitOfWork.CourseEnrollments
                .GetAllAsync(spec, cancellationToken);

            if (curriculumEnrollments == null || !curriculumEnrollments.Any())
            {
                _logger.LogInformation("No enrollments found for classroom {ClassroomId}.", request.ClassroomId);
                return new GrpcPagedStudentAssignmentsResponse
                {
                    TotalCount = 0,
                    PageNumber = request.PageRequest.PageNumber,
                    PageSize = request.PageRequest.PageSize,
                    TotalPages = 0
                };
            }

            // Flatten StudentAsm
            var studentAssignments = curriculumEnrollments
                .SelectMany(co => co.LessonProgress)
                .SelectMany(lp => lp.SectionProgress)
                .Select(sp => sp.StudentAssignment)
                .Where(sq => sq != null)
                .ToList();

            var grouped = studentAssignments.GroupBy(sq => sq!.AssignmentId).ToList();
            var assignmentStats = new List<GrpcAssignmentStatisticResponse>();

            foreach (var group in grouped)
            {
                var assignmentStat = await BuildAssignmentStatisticsAsync(group, cancellationToken);
                if (assignmentStat != null)
                    assignmentStats.Add(assignmentStat);
            }

            // Pagination
            var pageSize = request.PageRequest.PageSize;
            var pageNumber = request.PageRequest.PageNumber;
            var totalCount = assignmentStats.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var pagedItems = assignmentStats
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new GrpcPagedStudentAssignmentsResponse
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = { pagedItems }
            };
        }

        private async Task<GrpcAssignmentStatisticResponse?> BuildAssignmentStatisticsAsync(
           IGrouping<int, Domain.Entities.StudentAssignment> group,
           CancellationToken cancellationToken)
        {
            var assignment = await _grpcAssignmentClient.GetAssignmentByIdAsync(group.Key);
            if (assignment == null)
            {
                _logger.LogWarning("Assignment with ID {QuizId} not found.", group.Key);
                return null;
            }

            // Attempt cuối cùng mỗi học sinh
            var latestAttempts = group
                .Select(sq => sq!.AssignmentAttempts?
                    .OrderByDescending(qa => qa.SubmittedAt)
                    .FirstOrDefault())
                .Where(qa => qa != null)
                .Cast<Domain.Entities.AssignmentAttempt>()
                .ToList();

            var submissions = latestAttempts.Count;
            var averageScore = group.Average(sq => (double?)sq!.FinalScore) ?? 0;
            var passRate = submissions == 0
                ? 0
                : (double)group.Count(sq => sq.Status == StudentAssignmentStatus.Passed) / submissions * 100;

            // Build chi tiết
            var studentStats = await BuildStudentStatisticsAsync(group, assignment.Questions?.ToList() ?? [], cancellationToken);

            var assignmentStat = new GrpcAssignmentStatisticResponse
            {
                AssignmentId = group.Key,
                AssignmentTitle = assignment.Title,
                Submissions = submissions,
                AverageScore = (long?)averageScore,
                PassRate = (long?)passRate,
                TotalQuestions = assignment.Questions?.Count ?? 0,
            };

            assignmentStat.StudentStatistics.AddRange(studentStats);

            return assignmentStat;
        }

        private async Task<List<GrpcStudentAssignmentStatisticResponse>> BuildStudentStatisticsAsync(
           IGrouping<int, Domain.Entities.StudentAssignment> group, List<GrpcAssignmentQuestionModel> questions, CancellationToken cancellationToken)
        {
            var tasks = group.Select(async sq =>
            {
                var student = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(sq.StudentId), cancellationToken);
                var attempts = sq.AssignmentAttempts ?? [];
                var latestAttempt = sq.AssignmentAttempts?
                    .OrderByDescending(qa => qa.SubmittedAt)
                    .FirstOrDefault();

                return new GrpcStudentAssignmentStatisticResponse
                {
                    StudentId = sq.StudentId,
                    StudentName = student?.FullName ?? "Unknown",
                    ImageUrl = "",
                    TotalScore = (long?)sq.FinalScore,
                    Status = sq.Status.ToString(),
                    LastSubmittedAt = latestAttempt?.SubmittedAt.ToString("o"),
                    Attempts =
                    {
                        attempts.Select(a => a.ToGrpcAssignmentAttemptResponse())
                    }
                };
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}
