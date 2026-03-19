using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Specifications.Classrooms;
using Classroom.Application.Specifications.CourseEnrollments;
using DnsClient.Internal;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.ClassroomStudents.GetClassroomStudentById
{
    public class GetClassroomStudentByIdQueryHandler : IRequestHandler<GetClassroomStudentByIdQuery, GrpcClassroomStudentResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly ILogger<GetClassroomStudentByIdQueryHandler> _logger;
        private readonly IGrpcUserClient _grpcUserClient;
        public GetClassroomStudentByIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            ILogger<GetClassroomStudentByIdQueryHandler> logger,
            IGrpcUserClient grpcUserClient
            )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _grpcUserClient = grpcUserClient;
        }

        public async Task<GrpcClassroomStudentResponse> Handle(GetClassroomStudentByIdQuery request, CancellationToken cancellationToken)
        {
            var classroomStudentSpec = new ClassroomStudentSpecification(request.ClassroomId, request.StudentId);
            var classroomStudent = await _unitOfWork.ClassroomStudents.FirstOrDefaultAsync(classroomStudentSpec, cancellationToken);
            if (classroomStudent == null)
            {
                _logger.LogWarning("Classroom student not found. ClassroomId: {ClassroomId}, StudentId: {StudentId}", request.ClassroomId, request.StudentId);
                throw new KeyNotFoundException("Classroom student not found.");
            }

            var student = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(request.StudentId), cancellationToken);

            var courseEnrollmentSpec = new GetLatestActiveCourseEnrollmentSpecification(
                new Guid(request.StudentId.ToString()), classroomStudent.Classroom.CourseId, null, request.ClassroomId);
            var courseEnrollment = await _unitOfWork.CourseEnrollments
                                .FirstOrDefaultAsync(courseEnrollmentSpec, cancellationToken);
            if (courseEnrollment == null)
            {
                return new GrpcClassroomStudentResponse
                {
                    StudentId = request.StudentId,
                    StudentEmail = student.Email,
                    StudentName = student.FullName,
                    // ImageUrl might be null, so we provide an empty string if it is
                    StudentImageUrl = "",
                    CourseEnrollmentStatus = "NotEnrolled",
                    AverageAssignmentScore = 0.0,
                    AverageQuizScore = 0.0,
                    TotalAssignmentsSubmitted = 0,
                    TotalQuizzesTaken = 0
                };
            }
            var submittedAssignments = courseEnrollment.LessonProgress
                                    .SelectMany(lp => lp.SectionProgress)               
                                    .Where(sp => sp.StudentAssignment != null && 
                                           sp.StudentAssignment.Status != Domain.Enums.StudentAssignmentStatus.Assigned &&
                                           sp.StudentAssignment.Status != Domain.Enums.StudentAssignmentStatus.Expired)          
                                    .Count();

            double averageAssignmentScore = courseEnrollment.LessonProgress
                                        .SelectMany(lp => lp.SectionProgress)
                                        .Where(sp => sp.StudentAssignment != null &&
                                               sp.StudentAssignment.Status != Domain.Enums.StudentAssignmentStatus.Assigned)
                                        .Select(sp => sp.StudentAssignment?.FinalScore ?? 0)  
                                        .DefaultIfEmpty(0)
                                        .Average(x => (double)x);

            var submittedQuizzes = courseEnrollment.LessonProgress
                                .SelectMany(lp => lp.SectionProgress)
                                .Where(sp => sp.StudentQuiz != null &&
                                       sp.StudentQuiz.Status != Domain.Enums.StudentQuizStatus.Assigned &&
                                       sp.StudentQuiz.Status != Domain.Enums.StudentQuizStatus.Expired &&
                                       sp.StudentQuiz.Status != Domain.Enums.StudentQuizStatus.InProgress)
                                .Count();

            double averageQuizScore = courseEnrollment.LessonProgress
                                .SelectMany(lp => lp.SectionProgress)
                                .Where(sp => sp.StudentQuiz != null &&
                                       sp.StudentQuiz.Status != Domain.Enums.StudentQuizStatus.Assigned &&
                                       sp.StudentQuiz.Status != Domain.Enums.StudentQuizStatus.InProgress)
                                .Select(sp => sp.StudentQuiz?.FinalScore ?? 0)
                                .DefaultIfEmpty(0)
                                .Average(x => (double)x);

            return new GrpcClassroomStudentResponse
            {
                StudentId = request.StudentId,
                StudentEmail = student.Email,
                StudentName = student.FullName,
                // ImageUrl might be null, so we provide an empty string if it is
                StudentImageUrl = "",
                CourseEnrollmentStatus = courseEnrollment.Status.ToString(),
                AverageAssignmentScore = averageAssignmentScore,
                AverageQuizScore = averageQuizScore,
                TotalAssignmentsSubmitted = submittedAssignments,
                TotalQuizzesTaken = submittedQuizzes
            };
        }
    }
}
