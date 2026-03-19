using Identity.Application.Common.Interfaces;
using Identity.Application.Specifications.Contacts;
using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Queries.GetContactById
{
    public class GetContactByIdQueryHandler : IRequestHandler<GetContactByIdQuery, ContactResponse>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;
        public GetContactByIdQueryHandler(IIdentityUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ContactResponse> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            var spec = new GetContactByIdSpecification(request.Id);
            var contact = await _unitOfWork.Contacts.FirstOrDefaultAsync(spec, cancellationToken);
            var response = new ContactResponse
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                OrganizationName = contact.OrganizationName,
                JobRole = contact.JobRole.Name,
                Status = contact.Status.ToString(),
                CreatedAt = contact.CreatedDate.ToString("o"),
                UpdatedAt = contact.LastModifiedDate?.ToString("o") ?? string.Empty,
                JobRoleId = contact.JobRoleId
            };
            return response;
        }
    }
}
