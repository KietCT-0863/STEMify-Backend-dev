using Classroom.Application.Extensions.Mapping;
using Classroom.Application.Features.Classrooms.Commands.DeleteClassroom;
using Classroom.Application.Features.Classrooms.Queries.GetClassroomById;
using Classroom.Application.Features.Classrooms.Queries.GetClassroomLearningSnapshot;
using Classroom.Application.Features.Classrooms.Queries.GetClassroomSchedule;
using Classroom.Application.Features.Classrooms.Queries.GetClassroomStatistic;
using Classroom.Application.Queries.Classrooms;
using Grpc.Core;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class ClassroomGrpcService : GrpcClassroom.GrpcClassroomBase
    {
        private readonly IMediator _mediator;

        public ClassroomGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcPagedClassroomsResponse> GetPagedClassrooms(
            GetClassroomsRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain params
            var queryParams = request.ToClassroomParams();

            var result = await _mediator.Send(new GetClassroomListQuery(queryParams));

            // Map result domain → gRPC
            var response = new GrpcPagedClassroomsResponse
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
            };
            response.Items.AddRange(result.Items.Select(c => c.ToGrpcClassroomModel()));

            return response;
        }

        public override async Task<GrpcClassroomResponse> GetClassroomById(
            GetClassroomRequest request,
            ServerCallContext context
        )
        {
            var result = await _mediator.Send(new GetClassroomByIdQuery(request.Id));
            // Map result domain → gRPC
            var response = result.ToGrpcClassroomModel();
            return response;
        }

        public override async Task<GrpcCreateClassroomResponse> CreateClassroom(
            CreateClassroomRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = request.ToCreateClassroomCommand();
            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcClassroomResponse> UpdateClassroom(
            UpdateClassroomRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = request.ToUpdateClassroomCommand();
            var result = await _mediator.Send(command);
            // Map result domain → gRPC
            var response = result.ToGrpcClassroomModel();
            return response;
        }

        public override async Task<DeleteClassroomResponse> DeleteClassroom(
            DeleteClassroomRequest request,
            ServerCallContext context
        )
        {
            // Map request gRPC → domain model
            var command = new DeleteClassroomCommand(request.Id);
            var result = await _mediator.Send(command);

            var response = new DeleteClassroomResponse { Success = result };
            return response;
        }

        public async override Task<GrpcClassroomScheduleResponse> GetClassroomSchedule(GetClassroomRequest request, ServerCallContext context)
        {
            var query = new GetClassroomScheduleQuery
            {
                ClassroomId = request.Id
            };
            return await _mediator.Send(query);
        }

        public async override Task<GrpcClassroomStatisticResponse> GetClassroomStatistic(GetClassroomRequest request, ServerCallContext context)
        {
            var query = new GetClassroomStatisticQuery
            {
                ClassroomId = request.Id
            };
            return await _mediator.Send(query);
        }

        public async override Task<GrpcClassroomLearningSnapshotResponse> GetClassroomLearningSnapshot(
            GetClassroomLearningSnapshotRequest request,
            ServerCallContext context)
        {
            var query = new GetClassroomLearningSnapshotQuery
            {
                ClassroomId = request.ClassroomId,
                StudentId = request.StudentId,
                DaysBack = request.DaysBack
            };
            return await _mediator.Send(query);
        }
    }
}
