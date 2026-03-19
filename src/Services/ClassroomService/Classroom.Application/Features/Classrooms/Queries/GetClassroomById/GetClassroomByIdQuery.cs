using Classroom.Application.Models.ClassroomModels;
using MediatR;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomById
{
    public class GetClassroomByIdQuery(int id) : IRequest<ClassroomModel>
    {
        public int Id { get; } = id;
    }
}
