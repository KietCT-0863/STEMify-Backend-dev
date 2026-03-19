using Identity.Application.Common.Interfaces;
using Identity.Application.Users.Commands.CreateContact;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.UpdateContact
{
    public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, ContactResponse>
    {
        private readonly IIdentityUnitOfWork _unitOfWork;
        private readonly ILogger<CreateContactCommandHandler> _logger;
        public UpdateContactCommandHandler(IIdentityUnitOfWork unitOfWork, ILogger<CreateContactCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<ContactResponse> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            var contact = await _unitOfWork.Contacts.FindByIdAsync(request.Id);
            if (contact == null) 
                throw new KeyNotFoundException($"Contact with ID {request.Id} not found.");

            // Update fields if they are provided in the request
            if (request.FirstName != null)
                contact.FirstName = request.FirstName;
            if (request.LastName != null)
                contact.LastName = request.LastName;
            if (request.Email != null)
                contact.Email = request.Email;
            if (request.PhoneNumber != null)
                contact.PhoneNumber = request.PhoneNumber;
            if (request.OrganizationName != null)
                contact.OrganizationName = request.OrganizationName;
            if (request.JobRoleId.HasValue)
                contact.JobRoleId = request.JobRoleId.Value;
            if (request.Status.HasValue)
                contact.Status = request.Status.Value;

            contact.LastModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Contacts.UpdateAsync(contact, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Contact updated with ID: {ContactId}", contact.Id);
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
