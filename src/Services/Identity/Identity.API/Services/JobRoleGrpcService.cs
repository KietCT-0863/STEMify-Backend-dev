using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Identity.Application.Users.Commands.CreateJobRole;
using Identity.Application.Users.Queries.GetJobRoles;
using MediatR;
using Shared.Protos.User;

namespace Identity.API.Services
{
    public class JobRoleGrpcService : JobRoleService.JobRoleServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<JobRoleGrpcService> _logger;
        public JobRoleGrpcService(IMediator mediator, ILogger<JobRoleGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<PagedJobRoleList> GetAllJobRoles(Empty request, Grpc.Core.ServerCallContext context)
        {
            _logger.LogInformation("Received GetAllJobRoles gRPC request");
            var query = new GetJobRolesQuery();
            var response = await _mediator.Send(query);
            return response;
        }

        public override async Task<Empty> CreateJobRole(CreateJobRoleRequest request, ServerCallContext context)
        {
            var command = new CreateJobRoleCommand
            {
                Names = request.Names.ToList()
            };
            await _mediator.Send(command);
            return new Empty();
        }
    }
}
