using Classroom.Application.Models.ClassroomModels;
using Classroom.Application.Specifications.Classrooms;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Queries.Classrooms
{
    public class GetClassroomListQuery(ClassroomParams classroomParams)
        : IRequest<PageList<ClassroomModel>>
    {
        public ClassroomParams ClassroomParams { get; set; } = classroomParams;
    }
}
