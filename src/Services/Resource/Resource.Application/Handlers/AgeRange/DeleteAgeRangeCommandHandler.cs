using MediatR;
using Resource.Application.Commands.AgeRange;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.AgeRange
{
    public class DeleteAgeRangeCommandHandler : IRequestHandler<DeleteAgeRangeCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteAgeRangeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteAgeRangeCommand request, CancellationToken cancellationToken)
        {
            var ageRange = await _unitOfWork.AgeRanges.FindByIdForUpdateAsync(
                request.Id,
                cancellationToken
            );
            if (ageRange == null)
                throw new KeyNotFoundException($"AgeRange with ID {request.Id} not found.");

            await _unitOfWork.AgeRanges.DeleteAsync(ageRange, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
