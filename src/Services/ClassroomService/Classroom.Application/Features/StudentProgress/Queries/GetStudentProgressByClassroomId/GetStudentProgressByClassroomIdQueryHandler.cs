using Classroom.Application.Common.Interfaces;
using Classroom.Application.Common.Interfaces.Grpc;
using Classroom.Application.Specifications.CourseEnrollments;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentProgress.Queries.GetStudentProgressByClassroomId
{
    public class GetStudentProgressByClassroomIdQueryHandler :
        IRequestHandler<GetStudentProgressByClassroomIdQuery, GrpcClassroomStudentProgressResponse>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;
        private readonly IGrpcCourseClient _grpcCourseClient;
        private readonly IGrpcUserClient _grpcUserClient;
        public GetStudentProgressByClassroomIdQueryHandler(
            IClassroomUnitOfWork unitOfWork,
            IGrpcCourseClient grpcCourseClient,
            IGrpcUserClient grpcUserClient)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _grpcCourseClient = grpcCourseClient ?? throw new ArgumentNullException(nameof(grpcCourseClient));
            _grpcUserClient = grpcUserClient;
        }
        public async Task<GrpcClassroomStudentProgressResponse> Handle(
        GetStudentProgressByClassroomIdQuery request,
        CancellationToken cancellationToken)
        {
            // Get all student progress in a classroom for a specific course
            var spec = new GetStudentCourseProgressByClassroomIdSpecification(request.ClassroomId, request.CourseId);
            var courseEnrollments = await _unitOfWork.CourseEnrollments.GetAllAsync(spec, cancellationToken);

            // get all students in the classroom
            var classroomStudents = await _unitOfWork.ClassroomStudents
                .FindAsync(cs => cs.ClassroomId == request.ClassroomId, cancellationToken);

            // call grpc to get course details
            var course = await _grpcCourseClient.GetCourseByIdAsync(request.CourseId);
            var grpcLessons = course.Lessons.Select(lesson => new Lesson
            {
                LessonId = lesson.Id,
                LessonTitle = lesson.Title,
                SectionIds = { lesson.SectionIds }
            });

            // get all student info
            var allStudentIds = classroomStudents.Select(cs => cs.StudentId).Distinct().ToList();
            var userTasks = allStudentIds.Select(async studentId =>
            {
                var user = await _grpcUserClient.GetOrganizationUserByIdAsync(Guid.Parse(studentId), cancellationToken);
                return new { studentId, fullName = user?.FullName ?? string.Empty };
            }).ToList();

            var userResults = await Task.WhenAll(userTasks);
            var studentUsers = userResults.ToDictionary(x => x.studentId, x => x.fullName);

            // Mapping student progress
            var enrollmentMap = courseEnrollments.ToDictionary(e => e.StudentId);

            var studentProgressList = classroomStudents.Select(cs =>
            {
                var studentId = cs.StudentId;
                var studentName = studentUsers.TryGetValue(studentId, out var name) ? name : string.Empty;

                if (enrollmentMap.TryGetValue(Guid.Parse(studentId), out var enrollment))
                {
                    return new Shared.Protos.Classroom.StudentProgress
                    {
                        StudentId = studentId.ToString(),
                        StudentName = studentName,
                        CourseEnrollmentId = enrollment.Id,
                        LessonProgresses = {
                        enrollment.LessonProgress.Select(lp => new LessonProgressModel
                        {
                            Id = lp.Id,
                            LessonId = lp.LessonId,
                            Status = lp.Status.ToString(),
                            CompletedAt = lp.CompletedAt?.ToTimestamp(),
                            SectionProgresses = {
                                lp.SectionProgress.Select(sp => new SectionProgressModel
                                {
                                    Id = sp.Id,
                                    SectionId = sp.SectionId,
                                    Status = sp.Status.ToString(),
                                    CompletedAt = sp.CompletedAt?.ToTimestamp(),
                                })
                            }
                        })
                    }
                    };
                }

                return new Shared.Protos.Classroom.StudentProgress
                {
                    StudentId = studentId.ToString(),
                    StudentName = studentName,
                    CourseEnrollmentId = 0,
                    LessonProgresses = { }
                };
            }).ToList();

            return new GrpcClassroomStudentProgressResponse
            {
                CourseId = request.CourseId,
                ClassroomId = request.ClassroomId,
                Lessons = { grpcLessons },
                StudentProgress = { studentProgressList }
            };
        }

    }
}
