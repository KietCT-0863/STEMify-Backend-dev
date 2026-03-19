using Google.Protobuf.WellKnownTypes;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Application.Specifications;
using Shared.Protos.Order;

namespace Order.Application.Queries.Contracts.GetContractById
{
    public class GetContractByIdQueryHandler
        : IRequestHandler<GetContractByIdQuery, GrpcContractDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public GetContractByIdQueryHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcContractDetail> Handle(
            GetContractByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new ContractByIdSpecification(request.Id);

            var contract = await _unitOfWork.Contracts.FirstOrDefaultAsync(spec, cancellationToken);

            if (contract == null)
            {
                throw new KeyNotFoundException($"Contract with ID {request.Id} not found.");
            }

            var grpcContract = new GrpcContractDetail
            {
                Id = contract.Id,
                Name = contract.Name,
                Description = contract.Description ?? string.Empty,
                Status = contract.Status.ToString(),
                FileUrl = contract.FileUrl ?? string.Empty,
                Organization = new GrpcOrganizationInformation
                {
                    Id = contract.Organization.Id,
                    Name = contract.Organization.Name,
                    ImageUrl = contract.Organization.ImageUrl ?? string.Empty,
                    OrganizationType = contract.Organization.OrganizationType.Name,
                },
                CreatedDate = Timestamp.FromDateTimeOffset(contract.CreatedDate),
                LastModifiedDate = contract.LastModifiedDate.HasValue
                        ? Timestamp.FromDateTimeOffset(contract.LastModifiedDate.Value)
                        : null
            };

            return grpcContract;
        }
    }
}