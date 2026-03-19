using MediatR;

namespace Resource.Application.Commands.LessonAsset
{
    public class DeleteLessonAssetsCommand : IRequest<bool>
    {
        public List<int> DeletedAssetIds { get; set; } = [];
    }
}
