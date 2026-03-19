using MediatR;
using Resource.Application.Commands.Standard;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Standard
{
    public class CreateStandardCommandHandler
        : IRequestHandler<CreateStandardCommand, StandardResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateStandardCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StandardResponse> Handle(
            CreateStandardCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var standard = new Domain.Entities.Standard
                {
                    Name = request.StandardName,
                    Description = request.Description
                };

                await _unitOfWork.Standards.AddAsync(standard, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new StandardResponse()
                {
                    Id = standard.Id,
                    StandardName = standard.Name,
                    Description = standard.Description
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while creating the Standard: {ex.Message}",
                    ex
                );
            }
        }
    }
}
