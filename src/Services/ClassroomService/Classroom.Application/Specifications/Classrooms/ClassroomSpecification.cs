using Ardalis.Specification;
using Classroom.Domain.Enums;

namespace Classroom.Application.Specifications.Classrooms
{
    public class ClassroomSpecification : Specification<Domain.Entities.Classroom>
    {
        public ClassroomSpecification(ClassroomParams classroomParams)
        {
            // Apply filter
            if (!string.IsNullOrEmpty(classroomParams.Search))
                Query.Where(c => c.Name.ToLower().Contains(classroomParams.Search) || c.ClassCode.ToLower().Contains(classroomParams.Search));

            if (classroomParams.TeacherId.HasValue)
                Query.Where(c => c.TeacherId == classroomParams.TeacherId);
            if (classroomParams.CourseId.HasValue)
                Query.Where(c => c.CourseId == classroomParams.CourseId);
            if (classroomParams.OrganizationId.HasValue)
                Query.Where(c => c.OrganizationId == classroomParams.OrganizationId);
            if (classroomParams.OrganizationSubscriptionOrderId.HasValue)
                Query.Where(c => c.OrganizationSubscriptionOrderId == classroomParams.OrganizationSubscriptionOrderId);

            if (classroomParams.FromDate.HasValue)
                Query.Where(c => c.CreatedAt >= classroomParams.FromDate.Value);
            if (classroomParams.ToDate.HasValue)
                Query.Where(c => c.CreatedAt <= classroomParams.ToDate.Value);

            if (!string.IsNullOrEmpty(classroomParams.StudentId))
                Query.Where(c => c.ClassroomStudents.Any(cs => cs.StudentId == classroomParams.StudentId));

            // Apply filter and sorting
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            switch (classroomParams.Status?.ToLower())
            {
                case "upcoming":
                    Query
                        .Where(c => c.Status == ClassroomStatus.Pending)
                        .OrderBy(c => c.StartDate);
                    break;
                case "endsoon":
                    Query
                        .Where(c => c.Status == ClassroomStatus.InProgress)
                        .OrderBy(c => c.EndDate);
                    break;
                case "inprogress":
                    Query
                        .Where(c => c.Status == ClassroomStatus.InProgress)
                        .OrderBy(c => c.StartDate);
                    break;
                case "completed":
                    Query
                        .Where(c => c.Status == ClassroomStatus.Completed)
                        .OrderByDescending(c => c.EndDate);
                    break;
                default:
                    Query
                        .Where(c => c.Status != ClassroomStatus.Deleted)
                        .OrderByDescending(c => c.StartDate);
                    break;
            }

            // apply include
            Query.Include(c => c.ClassroomStudents);

            // Apply pagination
            Query
                .Skip((classroomParams.PageNumber - 1) * classroomParams.PageSize)
                .Take(classroomParams.PageSize);
        }
    }

    public class ClassroomByIdSpecification : Specification<Domain.Entities.Classroom>
    {
        public ClassroomByIdSpecification(int id)
        {
            Query.Where(c => c.Id == id && c.Status != ClassroomStatus.Deleted)
                .Include(c => c.ClassroomStudents);
        }
    }
}
