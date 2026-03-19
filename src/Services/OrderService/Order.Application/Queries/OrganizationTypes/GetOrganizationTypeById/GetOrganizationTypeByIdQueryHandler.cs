using MediatR;
using Order.Application.Common.Interfaces;
using Shared.Protos.Order;

namespace Order.Application.Queries.OrganizationTypes.GetOrganizationTypeById
{
    public class GetOrganizationTypeByIdQueryHandler
        : IRequestHandler<GetOrganizationTypeByIdQuery, GrpcOrganizationTypeModel>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public GetOrganizationTypeByIdQueryHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcOrganizationTypeModel> Handle(
            GetOrganizationTypeByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var organizationType = await _unitOfWork.OrganizationTypes.FindByIdAsync(request.Id, cancellationToken);

            if (organizationType == null)
            {
                throw new KeyNotFoundException($"OrganizationType with ID {request.Id} not found.");
            }

            var grpcOrganizationType = new GrpcOrganizationTypeModel
            {
                Id = organizationType.Id,
                Name = organizationType.Name,
            };

            return grpcOrganizationType;
        }
    }
}