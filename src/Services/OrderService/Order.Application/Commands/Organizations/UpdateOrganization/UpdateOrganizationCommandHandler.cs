using Contracts.Abstractions.Services;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Queries.Organizations.GetOrganizationById;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Order;

namespace Order.Application.Commands.Organizations.UpdateOrganization
{
    public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, GrpcOrganizationDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMediator _mediator;

        public UpdateOrganizationCommandHandler(IOrderUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mediator = mediator;
        }

        public async Task<GrpcOrganizationDetail> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var organization = await _unitOfWork.Organizations.FindByIdAsync(request.Id, cancellationToken);
            if (organization == null)
                throw new KeyNotFoundException($"Organization with ID {request.Id} not found.");

            if (request.OrganizationTypeId.HasValue)
            {
                var orgType = await _unitOfWork.OrganizationTypes.FindByIdAsync(request.OrganizationTypeId.Value, cancellationToken);
                if (orgType == null)
                    throw new KeyNotFoundException($"OrganizationType with ID {request.OrganizationTypeId.Value} not found.");

                organization.OrganizationTypeId = request.OrganizationTypeId.Value;
            }

            if (request.Name != null)
                organization.Name = request.Name;

            if (request.Description != null)
                organization.Description = request.Description;

            if (request.Status.HasValue)
                organization.Status = request.Status.Value;

            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{organization.Name}-{Guid.NewGuid()}",
                };

                var uploadResult = await _cloudinaryService.UploadImageAsync(uploadRequest);
                if (uploadResult != null)
                {
                    organization.ImageUrl = uploadResult.AssetUrl;
                }
            }

            organization.LastModifiedDate = DateTimeOffset.UtcNow;

            await _unitOfWork.Organizations.UpdateAsync(organization, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var query = new GetOrganizationByIdQuery
            {
                Id = organization.Id
            };
            return await _mediator.Send(query, cancellationToken);
        }
    }
}