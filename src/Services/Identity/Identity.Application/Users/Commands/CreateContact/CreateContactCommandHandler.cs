using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.CreateContact
{
    public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, ContactResponse>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly ILogger<CreateContactCommandHandler> _logger;
        public CreateContactCommandHandler(IIdentityUnitOfWork unitOfWork, ILogger<CreateContactCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<ContactResponse> Handle(CreateContactCommand request, CancellationToken cancellationToken)
        {
            var contact = new Contact
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                OrganizationName = request.OrganizationName,
                JobRoleId = request.JobRoleId,
                Status = Domain.Enums.ContactStatus.New,
                CreatedDate = DateTime.UtcNow,
            };
            await _unitOfWork.Contacts.AddAsync(contact, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Contact created with ID: {ContactId}", contact.Id);
            return new ContactResponse
            {
                Id = contact.Id,
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                PhoneNumber = contact.PhoneNumber,
                OrganizationName = contact.OrganizationName,
                JobRoleId = contact.JobRoleId,
                Status = contact.Status.ToString()
            };
        }
    }
}
