using MediatR;
using Resource.Application.Commands.Standard;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Standard
{
    public class DeleteStandardCommandHandler : IRequestHandler<DeleteStandardCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteStandardCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteStandardCommand request, CancellationToken cancellationToken)
        {
            var standard = await _unitOfWork.Standards.FindByIdForUpdateAsync(
                request.Id,
                cancellationToken
            );
            if (standard == null)
                throw new KeyNotFoundException($"Standard with ID {request.Id} not found.");

            await _unitOfWork.Standards.DeleteAsync(standard, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
