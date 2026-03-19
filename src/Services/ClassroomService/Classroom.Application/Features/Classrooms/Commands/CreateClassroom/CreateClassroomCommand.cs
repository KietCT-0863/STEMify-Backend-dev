using Classroom.Application.Models.ClassroomModels;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Classrooms.Commands.CreateClassroom
{
    public class CreateClassroomCommand : IRequest<GrpcCreateClassroomResponse>
    {
        public int CourseId { get; set; }
        public List<StudentGroup> StudentGroups { get; set; } =[];
        public int OrganizationSubscriptionOrderId { get; set; }
        public string? Description { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? CoverImageUrl { get; set; }
    }

    public class StudentGroup
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public List<string> StudentIds { get; set; } = [];
        public Guid TeacherId { get; set; }
        public string Grade { get; set; } = string.Empty;
    }
}
