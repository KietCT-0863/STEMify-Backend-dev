using Classroom.Application.Models.EnrollmentModels;

namespace Classroom.Application.Models.ClassroomModels
{
    public class ClassroomModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int OrganizationSubscriptionOrderId { get; set; }
        public int OrganizationId { get; set; }
        public CourseModel Course { get; set; } = new CourseModel();
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public UserModel Teacher { get; set; } = new UserModel();
        public string ClassCode { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public int NumberOfStudents { get; set; }
        public IEnumerable<UserModel> Students { get; set; } = [];
    }
}
