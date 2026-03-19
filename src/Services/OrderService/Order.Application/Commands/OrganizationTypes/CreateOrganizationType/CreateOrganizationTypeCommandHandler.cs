using MediatR;
using Order.Application.Common.Interfaces;
using Shared.Protos.Order;

namespace Order.Application.Commands.OrganizationTypes.CreateOrganizationType
{
    public class CreateOrganizationTypeCommandHandler : IRequestHandler<CreateOrganizationTypeCommand, GrpcOrganizationTypeModel>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public CreateOrganizationTypeCommandHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcOrganizationTypeModel> Handle(CreateOrganizationTypeCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Organization type name is required.", nameof(request.Name));

            var exists = await _unitOfWork.OrganizationTypes
                .AnyAsync(ot => ot.Name.ToLower() == name.ToLower(), cancellationToken);
            if (exists)
                throw new InvalidOperationException($"Organization type with name '{name}' already exists.");

            var organizationType = new Domain.Entities.OrganizationType
            {
                Name = name,
            };

            await _unitOfWork.OrganizationTypes.AddAsync(organizationType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new GrpcOrganizationTypeModel
            {
                Id = organizationType.Id,
                Name = organizationType.Name,
            };

            return response;
        }
    }
}