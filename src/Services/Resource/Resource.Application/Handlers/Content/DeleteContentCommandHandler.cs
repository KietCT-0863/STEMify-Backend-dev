using MediatR;
using Resource.Application.Commands.Content;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Content
{
    public class DeleteContentCommandHandler : IRequestHandler<DeleteContentCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteContentCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteContentCommand request, CancellationToken cancellationToken)
        {
            var content = await _unitOfWork.Contents.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (content == null)
                throw new KeyNotFoundException($"Content with ID {request.Id} not found.");

            await _unitOfWork.Contents.DeleteAsync(content, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
