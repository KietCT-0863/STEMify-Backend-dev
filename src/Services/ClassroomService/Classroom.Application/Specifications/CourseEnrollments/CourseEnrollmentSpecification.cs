using Ardalis.Specification;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;

namespace Classroom.Application.Specifications.CourseEnrollments
{
    public class GetLatestActiveCourseEnrollmentSpecification : Specification<CourseEnrollment>
    {
        public GetLatestActiveCourseEnrollmentSpecification(
            Guid studentId, int courseId, int? curriculumEnrollmentId = null, int? classroomId = null)
        {
            Query
                .Where(e => e.StudentId == studentId
                         && e.CourseId == courseId
                         //&& (curriculumEnrollmentId.HasValue ? e.CurriculumEnrollmentId == curriculumEnrollmentId : !e.CurriculumEnrollmentId.HasValue)
                         && (classroomId.HasValue ? e.ClassroomId == classroomId : e.ClassroomId == null)
                         && e.Status != EnrollmentStatus.Unenrolled)
                .Include(ce => ce.LessonProgress)
                    .ThenInclude(lp => lp.SectionProgress)
                        .ThenInclude(sp => sp.StudentAssignment)
                .Include(ce => ce.LessonProgress)
                    .ThenInclude(lp => lp.SectionProgress)
                        .ThenInclude(sp => sp.StudentQuiz)
                .OrderByDescending(e => e.EnrolledAt);
        }
    }

    public class GetCourseEnrollmentByIdSpecification : Specification<CourseEnrollment>
    {
        public GetCourseEnrollmentByIdSpecification(int enrollmentId)
        {
            Query
                .Where(e => e.Id == enrollmentId);
        }
    }
    public class GetCourseEnrollmentByClassroomIdSpecification : Specification<CourseEnrollment>
    {
        public GetCourseEnrollmentByClassroomIdSpecification(int classroomId)
        {
            Query
            .Where(ce => ce.ClassroomId == classroomId)
            .Include(ce => ce.CurriculumEnrollment!)
            .Include(ce => ce.LessonProgress)
                .ThenInclude(lp => lp.SectionProgress)
                    .ThenInclude(sp => sp.StudentQuiz!)
                        .ThenInclude(sq => sq.QuizAttempts)
                            .ThenInclude(qa => qa.QuestionAttempts)
                                .ThenInclude(qa => qa.AnswerAttempts)
            .Include(ce => ce.LessonProgress)
                .ThenInclude(lp => lp.SectionProgress)
                    .ThenInclude(sp => sp.StudentAssignment!)
                        .ThenInclude(sq => sq.AssignmentAttempts);
        }
    }

    public class GetStudentCourseProgressByClassroomIdSpecification : Specification<CourseEnrollment>
    {
        public GetStudentCourseProgressByClassroomIdSpecification(int classroomId, int courseId)
        {
            Query
            .Where(ce => ce.ClassroomId == classroomId && ce.CourseId == courseId)
            .Include(ce => ce.CurriculumEnrollment!)
            .Include(ce => ce.LessonProgress)
                .ThenInclude(lp => lp.SectionProgress);
        }
    }
}
