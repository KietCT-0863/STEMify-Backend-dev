using MediatR;
using Resource.Application.Commands.Standard;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Standards;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Standard
{
    public class UpdateStandardCommandHandler
        : IRequestHandler<UpdateStandardCommand, StandardResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateStandardCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StandardResponse> Handle(
            UpdateStandardCommand request,
            CancellationToken cancellationToken
        )
        {
                var spec = new StandardByIdSpecification(request.Id);
                var standard = await _unitOfWork.Standards.FirstOrDefaultAsync(
                    spec,
                    cancellationToken
                );
                if (standard == null)
                    throw new KeyNotFoundException($"Standard with ID {request.Id} not found.");

                if (!string.IsNullOrEmpty(request.StandardName))
                    standard.Name = request.StandardName;
                if (!string.IsNullOrEmpty(request.Description))
                    standard.Description = request.Description;

                await _unitOfWork.Standards.UpdateAsync(standard, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new StandardResponse()
                {
                    Id = standard.Id,
                    StandardName = standard.Name,
                    Description = standard.Description
                };

                return response;
        }
    }
}
