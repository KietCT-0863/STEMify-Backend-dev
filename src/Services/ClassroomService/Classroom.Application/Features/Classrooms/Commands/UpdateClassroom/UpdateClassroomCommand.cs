using Classroom.Application.Models.ClassroomModels;
using MediatR;

namespace Classroom.Application.Features.Classrooms.Commands.UpdateClassroom
{
    public class UpdateClassroomCommand : IRequest<ClassroomModel>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? ClassCode { get; set; }
        public string? Grade { get; set; }
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? CoverImageUrl { get; set; }
        public Guid? TeacherId { get; set; }
        public int? CourseId { get; set; }
    }
}
