using MediatR;

namespace Resource.Application.Commands.LessonAsset
{
    public class DeleteLessonAssetTagsCommand : IRequest<bool>
    {
        public int LessonAssetId { get; set; }
        public List<int> TagIds { get; set; } = [];
    }
}
