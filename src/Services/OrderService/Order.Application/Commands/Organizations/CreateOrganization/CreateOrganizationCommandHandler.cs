using Contracts.Abstractions.Services;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Queries.Organizations.GetOrganizationById;
using Shared.DTOs.Cloudinary;
using Shared.Helper;
using Shared.Protos.Order;

namespace Order.Application.Commands.Organizations.CreateOrganization
{
    public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, GrpcOrganizationDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMediator _mediator;

        public CreateOrganizationCommandHandler(IOrderUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mediator = mediator;
        }

        public async Task<GrpcOrganizationDetail> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
        {
            var orgType = await _unitOfWork.OrganizationTypes.FindByIdAsync(request.OrganizationTypeId, cancellationToken);
            if (orgType == null)
                throw new KeyNotFoundException($"OrganizationType with ID {request.OrganizationTypeId} not found.");

            string code = CodeGeneratorHelper.GenerateOrganizationCode(request.Name);

            var organization = new Domain.Entities.Organization
            {
                Code = code,
                Name = request.Name,
                Description = request.Description,
                OrganizationTypeId = request.OrganizationTypeId,
            };

            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Name}-{Guid.NewGuid()}",
                };

                var uploadResult = await _cloudinaryService.UploadImageAsync(uploadRequest);
                if (uploadResult != null)
                {
                    organization.ImageUrl = uploadResult.AssetUrl;
                }
            }

            await _unitOfWork.Organizations.AddAsync(organization, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var query = new GetOrganizationByIdQuery
            {
                Id = organization.Id
            };
            return await _mediator.Send(query, cancellationToken);
        }
    }
}