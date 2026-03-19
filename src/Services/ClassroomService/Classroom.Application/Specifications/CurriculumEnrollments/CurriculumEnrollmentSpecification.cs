using Ardalis.Specification;
using Classroom.Domain.Entities;
using Classroom.Domain.Enums;

namespace Classroom.Application.Specifications.CurriculumEnrollments
{
    public class GetCurriculumEnrollmentSpecification : Specification<CurriculumEnrollment>
    {
        public GetCurriculumEnrollmentSpecification(Guid studentId, int curriculumId)
        {
            Query
                .Where(e => e.StudentId == studentId
                         && e.CurriculumId == curriculumId
                         && e.Status == EnrollmentStatus.InProgress);
        }
    }

    public class GetCurriculumEnrollmentByIdSpecification : Specification<CurriculumEnrollment>
    {
        public GetCurriculumEnrollmentByIdSpecification(int enrollmentId)
        {
            Query
                .Where(e => e.Id == enrollmentId);
        }
    }

    public class GetCurriculumEnrollmentByClassroomIdSpecification : Specification<CurriculumEnrollment>
    {
        public GetCurriculumEnrollmentByClassroomIdSpecification(int classroomId)
        {
            Query
                .Where(e => e.CourseEnrollments != null && e.CourseEnrollments.Any(ce => ce.ClassroomId == classroomId))
                .Include(e => e.CourseEnrollments)
                    .ThenInclude(ce => ce.LessonProgress)
                        .ThenInclude(lp => lp.SectionProgress)
                            .ThenInclude(sp => sp.StudentQuiz!)
                                .ThenInclude(sq => sq.QuizAttempts)
                                    .ThenInclude(qa => qa.QuestionAttempts)
                                        .ThenInclude(qa => qa.AnswerAttempts)
                .Include(e => e.CourseEnrollments)
                    .ThenInclude(ce => ce.LessonProgress)
                        .ThenInclude(lp => lp.SectionProgress)
                            .ThenInclude(sp => sp.StudentAssignment!)
                                .ThenInclude(sa => sa.AssignmentAttempts);
        }
    }
}
