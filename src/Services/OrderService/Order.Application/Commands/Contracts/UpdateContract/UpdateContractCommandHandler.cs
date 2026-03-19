using Contracts.Abstractions.Services;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Queries.Contracts.GetContractById;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Order;

namespace Order.Application.Commands.Contracts.UpdateContract
{
    public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand, GrpcContractDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMediator _mediator;

        public UpdateContractCommandHandler(IOrderUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _mediator = mediator;
        }

        public async Task<GrpcContractDetail> Handle(UpdateContractCommand request, CancellationToken cancellationToken)
        {
            var contract = await _unitOfWork.Contracts.FindByIdAsync(request.Id, cancellationToken);
            if (contract == null)
                throw new KeyNotFoundException($"Contract with ID {request.Id} not found.");

            //if (request.OrganizationId.HasValue)
            //{
            //    var org = await _unitOfWork.Contracts.FindByIdAsync(request.OrganizationId.Value, cancellationToken);
            //    if (org == null)
            //        throw new KeyNotFoundException($"Organization with ID {request.OrganizationId.Value} not found.");

            //    contract.OrganizationId = request.OrganizationId.Value;
            //}

            if (request.Name != null)
                contract.Name = request.Name;

            if (request.Description != null)
                contract.Description = request.Description;

            if (request.Status.HasValue)
                contract.Status = request.Status.Value;

            if (request.FileBytes != null && request.FileBytes.Length > 0)
            {
                var uploadRequest = new UploadDocumentBytesRequest
                {
                    FileBytes = request.FileBytes,
                    FileName = $"{contract.Name}-{Guid.NewGuid()}",
                };

                var uploadResult = await _cloudinaryService.UploadDocumentAsync(uploadRequest);
                if (uploadResult != null)
                {
                    contract.FileUrl = uploadResult.AssetUrl;
                }
            }

            contract.LastModifiedDate = DateTimeOffset.UtcNow;

            await _unitOfWork.Contracts.UpdateAsync(contract, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var query = new GetContractByIdQuery
            {
                Id = contract.Id
            };
            return await _mediator.Send(query);
        }
    }
}