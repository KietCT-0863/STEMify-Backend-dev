using Ardalis.Specification;

namespace Classroom.Application.Specifications.Classrooms
{
    public class ClassroomStudentSpecification : Specification<Domain.Entities.ClassroomStudent>
    {
        public ClassroomStudentSpecification(int classroomId, string studentId)
        {
            Query.Where(cs => cs.ClassroomId == classroomId && cs.StudentId == studentId)
                 .Include(cs => cs.Classroom);
        }
    }
}
