using Classroom.Application.Features.Classrooms.Commands.CreateClassroomStudent;
using Classroom.Application.Features.Classrooms.Commands.DeleteClassroomStudent;
using Classroom.Application.Features.ClassroomStudents.GetClassroomStudentById;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class ClassroomStudentGrpcService : GrpcClassroomStudent.GrpcClassroomStudentBase
    {
        private readonly IMediator _mediator;
        public ClassroomStudentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }
        public override async Task<Empty> CreateClassroomStudent(CreateClassroomStudentRequest request, ServerCallContext context)
        {
            var command = new CreateClassroomStudentCommand
            {
                ClassroomId = request.ClassroomId,
                StudentIds = request.StudentIds.ToList(),
                StudentEmails = request.StudentEmails.ToList()
            };
            await _mediator.Send(command);
            return new Empty();
        }
        public override async Task<Empty> DeleteClassroomStudent(DeleteClassroomStudentRequest request, ServerCallContext context)
        {
            var command = new DeleteClassroomStudentCommand
            {
                ClassroomId = request.ClassroomId,
                StudentIds = request.StudentIds.ToList()
            };
            await _mediator.Send(command);
            return new Empty();
        }

        public override async Task<GrpcClassroomStudentResponse> GetClassroomStudentById(GetClassroomStudentByIdRequest request, ServerCallContext context)
        {
            
            var command = new GetClassroomStudentByIdQuery
            {
                ClassroomId = request.ClassroomId,
                StudentId = request.StudentId
            };
            return await _mediator.Send(command);
        }
    }
}
