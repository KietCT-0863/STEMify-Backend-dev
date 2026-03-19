using MediatR;
using Resource.Application.Commands.LessonAsset;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.LessonAsset
{
    public class DeleteLessonAssetsCommandHandler : IRequestHandler<DeleteLessonAssetsCommand, bool>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteLessonAssetsCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteLessonAssetsCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.LessonAssets.DeleteAsync(la => request.DeletedAssetIds.Contains(la.Id), cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0;
        }
    }
}
