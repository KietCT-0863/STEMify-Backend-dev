using MediatR;

namespace Resource.Application.Commands.LessonAsset
{
    public class CreateLessonAssetTagsCommand : IRequest<bool>
    {
        public int LessonAssetId { get; set; }
        public List<int>? TagIds { get; set; }
        public List<string>? TagNames { get; set; }

    }
}
