using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Specifications.StudentAssignment;
using Classroom.Domain.Enums;
using Contracts.Abstractions.Persistence;
using DnsClient.Internal;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentsByAssignmentId
{
    public class GetStudentAssignmentsByAssignmentIdQueryHandler : IRequestHandler<GetStudentAssignmentsByAssignmentIdQuery, GrpcAssignmentStatisticResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<GetStudentAssignmentsByAssignmentIdQueryHandler> _logger;
        private readonly IGrpcAssignmentClient _grpcAssignmentClient;
        private readonly IGrpcUserClient _grpcUserClient;
        public GetStudentAssignmentsByAssignmentIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcAssignmentClient grpcAssignmentClient,
            IGrpcUserClient grpcUserClient,
            ILogger<GetStudentAssignmentsByAssignmentIdQueryHandler> logger)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _grpcAssignmentClient = grpcAssignmentClient;
            _grpcUserClient = grpcUserClient;
        }
        public async Task<GrpcAssignmentStatisticResponse> Handle(GetStudentAssignmentsByAssignmentIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetStudentAssignmentsByAssignmentIdSpecification(request.AssignmentId, request.ClassroomId);
            var studentAssignments = await _unitOfWork.StudentAssignments.GetAllAsync(spec, cancellationToken);

            if (studentAssignments == null || !studentAssignments.Any())
            {
                _logger.LogInformation("No student assignments found for assignment {AssignmentId}.", request.AssignmentId);
                return new GrpcAssignmentStatisticResponse
                {
                    AssignmentId = request.AssignmentId,
                    AssignmentTitle = "N/A",
                    Submissions = 0,
                    AverageScore = 0,
                    PassRate = 0,
                    TotalQuestions = 0
                };
            }

            // Gọi Grpc để lấy thông tin Assignment
            var assignment = await _grpcAssignmentClient.GetAssignmentByIdAsync(request.AssignmentId);
            if (assignment == null)
            {
                _logger.LogWarning("Assignment with ID {AssignmentId} not found.", request.AssignmentId);
                return new GrpcAssignmentStatisticResponse
                {
                    AssignmentId = request.AssignmentId,
                    AssignmentTitle = "Unknown"
                };
            }

            // Latest attempt mỗi student
            var latestAttempts = studentAssignments
                .Select(sa => sa.AssignmentAttempts?
                    .OrderByDescending(attempt => attempt.SubmittedAt)
                    .FirstOrDefault())
                .Where(a => a != null)
                .Cast<Domain.Entities.AssignmentAttempt>()
                .ToList();

            var submissions = latestAttempts.Count;
            var averageScore = studentAssignments.Average(sa => (double?)sa.FinalScore) ?? 0;
            var passRate = submissions == 0
                ? 0
                : (double)studentAssignments.Count(sa => sa.Status == StudentAssignmentStatus.Passed) / submissions * 100;

            // Build chi tiết học sinh
            var studentStats = await BuildStudentStatisticsAsync(studentAssignments.ToList(), assignment.Questions?.ToList() ?? [], cancellationToken);

            return new GrpcAssignmentStatisticResponse
            {
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                Submissions = submissions,
                AverageScore = (long?)averageScore,
                PassRate = (long?)passRate,
                TotalQuestions = assignment.Questions?.Count ?? 0,
                StudentStatistics = { studentStats }
            };
        }

        private async Task<List<GrpcStudentAssignmentStatisticResponse>> BuildStudentStatisticsAsync(
           List<Domain.Entities.StudentAssignment> assignments, List<GrpcAssignmentQuestionModel> questions, CancellationToken cancellationToken)
        {
            var tasks = assignments.Select(async sa =>
            {
                var student = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(sa.StudentId), cancellationToken);
                var attempts = sa.AssignmentAttempts ?? [];
                var latestAttempt = attempts
                    .OrderByDescending(attempt => attempt.SubmittedAt)
                    .FirstOrDefault();

                return new GrpcStudentAssignmentStatisticResponse
                {
                    StudentId = sa.StudentId,
                    StudentName = student?.FullName ?? "Unknown",
                    ImageUrl = "",
                    TotalScore = (long?)sa.FinalScore,
                    Status = sa.Status.ToString(),
                    LastSubmittedAt = latestAttempt?.SubmittedAt.ToString("o"),
                    Attempts = { attempts.Select(a => a.ToGrpcAssignmentAttemptResponse()) }
                };
            });

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }
    }
}
