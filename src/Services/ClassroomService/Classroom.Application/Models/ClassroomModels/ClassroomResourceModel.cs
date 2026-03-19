namespace Classroom.Application.Models.ClassroomModels
{
    public class ClassroomResourceModel
    {
        public int Id { get; set; }
        public int ClassroomId { get; set; }
        public CourseModel Course { get; set; }
    }
}
