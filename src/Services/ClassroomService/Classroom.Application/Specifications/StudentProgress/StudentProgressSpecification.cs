using Ardalis.Specification;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;

namespace Classroom.Application.Specifications.StudentProgress
{
    public class StudentCompletedLessonProgressByEnrollmentSpec : Specification<StudentLessonProgress>
    {
        public StudentCompletedLessonProgressByEnrollmentSpec(int enrollmentId)
        {
            Query.Where(lp => lp.EnrollmentId == enrollmentId && lp.Status == ProgressStatus.Completed);
        }
    }

    public class GetStudentSectionProgressByIdSpec : Specification<StudentSectionProgress>
    {
        public GetStudentSectionProgressByIdSpec(int id)
        {
            Query.Where(sp => sp.Id == id)
                .Include(sp => sp.LessonProgress)
                    .ThenInclude(lp => lp.CourseEnrollment);
        }
    }
}
