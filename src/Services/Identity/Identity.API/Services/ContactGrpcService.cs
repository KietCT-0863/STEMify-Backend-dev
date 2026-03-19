using Grpc.Core;
using Identity.Application.Users.Commands.CreateContact;
using Identity.Application.Users.Commands.UpdateContact;
using Identity.Application.Users.Queries.GetContactById;
using Identity.Application.Users.Queries.GetContacts;
using Identity.Domain.Enums;
using MediatR;
using Shared.Extensions;
using Shared.Protos.User;

namespace Identity.API.Services
{
    public class ContactGrpcService : ContactService.ContactServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<JobRoleGrpcService> _logger;
        public ContactGrpcService(IMediator mediator, ILogger<JobRoleGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<ContactResponse> CreateContact(CreateContactRequest request, ServerCallContext context)
        {
            var command = new CreateContactCommand
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                OrganizationName = request.OrganizationName,
                JobRoleId = request.JobRoleId,
            };
            var response = await _mediator.Send(command);
            return response;
        }

        public override async Task<ContactResponse> UpdateContact(UpdateContactRequest request, ServerCallContext context)
        {
            var command = new UpdateContactCommand
            {
                Id = request.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                OrganizationName = request.OrganizationName,
                JobRoleId = request.JobRoleId,
                Status = request.Status.ToEnumOrNull<ContactStatus>()
            };
            var response = await _mediator.Send(command);
            return response;
        }

        public override async Task<PagedContactList> GetContacts(GetContactParams request, ServerCallContext context)
        {
            var query = new GetContactsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                Search = request.Search,
                Status = request.Status.ToEnumOrNull<ContactStatus>()
            };
            return await _mediator.Send(query);
        }

        public override async Task<ContactResponse> GetContactById(GetContactByIdRequest request, ServerCallContext context)
        {
            var query = new GetContactByIdQuery
            {
                Id = request.Id,
            };
            return await _mediator.Send(query);
        }
    }
}
