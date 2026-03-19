using Contracts.Abstractions.Services;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Queries.Contracts.GetContractById;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Order;

namespace Order.Application.Commands.Contracts.CreateContract
{
    public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, GrpcContractDetail>
    {
        private readonly IMediator _mediator;
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateContractCommandHandler(IOrderUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mediator = mediator;
        }

        public async Task<GrpcContractDetail> Handle(CreateContractCommand request, CancellationToken cancellationToken)
        {
            var org = await _unitOfWork.Organizations.FindByIdAsync(request.OrganizationId, cancellationToken);
            if (org == null)
                throw new KeyNotFoundException($"Organization with ID {request.OrganizationId} not found.");

            var contract = new Domain.Entities.Contract
            {
                Name = request.Name,
                Description = request.Description,
                OrganizationId = request.OrganizationId,
            };

            if (request.FileBytes != null && request.FileBytes.Length > 0)
            {
                var uploadRequest = new UploadDocumentBytesRequest
                {
                    FileBytes = request.FileBytes,
                    FileName = $"{request.Name}-{Guid.NewGuid()}",
                };

                var uploadResult = await _cloudinaryService.UploadDocumentAsync(uploadRequest);
                if (uploadResult != null)
                {
                    contract.FileUrl = uploadResult.AssetUrl;
                }
            }

            await _unitOfWork.Contracts.AddAsync(contract, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var query = new GetContractByIdQuery
            {
                Id = contract.Id
            };
            return await _mediator.Send(query);
        }
    }
}