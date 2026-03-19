using MediatR;
using Order.Application.Common.Interfaces;
using Shared.Protos.Order;

namespace Order.Application.Commands.OrganizationTypes.UpdateOrganizationType
{
    public class UpdateOrganizationTypeCommandHandler : IRequestHandler<UpdateOrganizationTypeCommand, GrpcOrganizationTypeModel>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public UpdateOrganizationTypeCommandHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcOrganizationTypeModel> Handle(UpdateOrganizationTypeCommand request, CancellationToken cancellationToken)
        {
            var organizationType = await _unitOfWork.OrganizationTypes.FindByIdAsync(request.Id, cancellationToken);
            if (organizationType == null)
                throw new KeyNotFoundException($"OrganizationType with ID {request.Id} not found.");

            if (request.Name != null)
            {
                var name = request.Name.Trim();
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Name cannot be empty or whitespace.", nameof(request.Name));

                var nameLower = name.ToLower();

                var exists = await _unitOfWork.OrganizationTypes
                    .AnyAsync(ot => ot.Id != organizationType.Id && ot.Name.ToLower() == nameLower, cancellationToken);

                if (exists)
                    throw new InvalidOperationException($"OrganizationType with name '{name}' already exists.");

                organizationType.Name = name;
            }

            await _unitOfWork.OrganizationTypes.UpdateAsync(organizationType, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new GrpcOrganizationTypeModel()
            {
                Id = organizationType.Id,
                Name = organizationType.Name,
            };

            return response;
        }
    }
}