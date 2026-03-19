using Ardalis.Specification;

namespace Classroom.Application.Specifications.StudentAssignment
{
    public class GetStudentAssignmentByIdSpecification : Specification<Domain.Entities.StudentAssignment>
    {
        public GetStudentAssignmentByIdSpecification(int id)
        {
            Query.Where(qa => qa.Id == id)
                 .Include(qa => qa.StudentSectionProgress)
                     .ThenInclude(sq => sq.LessonProgress)
                        .ThenInclude(qqa => qqa.CourseEnrollment)
                                .ThenInclude(qqa => qqa.Classroom)
                 .Include(qa => qa.AssignmentAttempts)
                     .ThenInclude(sq => sq.AssignmentQuestionAttempts)
                        .ThenInclude(qqa => qqa.RubricScores);
        }

    }
    public class GetStudentAssignmentsByAssignmentIdSpecification : Specification<Domain.Entities.StudentAssignment>
    {
        public GetStudentAssignmentsByAssignmentIdSpecification(int assignmentId, int classroomId)
        {
            Query.Where(a => a.AssignmentId == assignmentId &&
            a.StudentSectionProgress.LessonProgress.CourseEnrollment.ClassroomId == classroomId)
                .Include(a => a.AssignmentAttempts)
                .Include(a => a.StudentSectionProgress)
                    .ThenInclude(sp => sp.LessonProgress)
                        .ThenInclude(lp => lp.CourseEnrollment);
        }
    }
}
