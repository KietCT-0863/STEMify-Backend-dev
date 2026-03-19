using MediatR;
using Resource.Application.Commands.LessonAsset;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.LessonAsset
{
    public class DeleteLessonAssetTagsCommandHandler : IRequestHandler<DeleteLessonAssetTagsCommand, bool>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteLessonAssetTagsCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<bool> Handle(DeleteLessonAssetTagsCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.LessonAssetTags.DeleteAsync
                (lat => lat.LessonAssetId == request.LessonAssetId
                && request.TagIds.Contains(lat.TagId), cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            return result > 0;
        }
    }
}
