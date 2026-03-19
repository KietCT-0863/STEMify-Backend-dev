using Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentById;
using Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentsByAssignmentId;
using Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentsByClassroomId;
using Classroom.Domain.Enums;
using Grpc.Core;
using Infrastructure.Common.Paging;
using MediatR;
using Shared.Extensions;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class StudentAssignmentGrpcService : GrpcStudentAssignment.GrpcStudentAssignmentBase
    {
        private readonly IMediator _mediator;
        public StudentAssignmentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcStudentAssignmentResponse> GetStudentAssignmentById(GetStudentAssignmentByIdRequest request, ServerCallContext context)
        {
            var query = new GetStudentAssignmentByIdQuery
            {
                Id = request.Id
            };
            return await _mediator.Send(query);
        }

        public async override Task<GrpcPagedStudentAssignmentsResponse> GetPagedStudentAssignmentByClassroom(GetStudentAssignmentParams request, ServerCallContext context)
        {
            var query = new GetStudentAssignmentByClassroomIdQuery
            {
                ClassroomId = request.ClassroomId,
                Status = request.Status.ToEnumOrNull<StudentAssignmentStatus>(),
                PageRequest = new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10
                }
            };
            return await _mediator.Send(query);
        }

        public override async Task<GrpcAssignmentStatisticResponse> GetStudentAssignmentByAssignmentId(GetStudentAssignmentByAssignmentIdRequest request, ServerCallContext context)
        {
            var query = new GetStudentAssignmentsByAssignmentIdQuery
            {
                AssignmentId = request.AssignmentId,
                ClassroomId = request.ClassroomId
            };
            return await _mediator.Send(query);
        }
    }
}
