using MediatR;
using Resource.Application.Models.Lesson;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.LessonAsset
{
    public class CreateLessonAssetsCommand : IRequest<CreateLessonAssetsResponse>
    {
        public List<CreateLessonAssetRequest> Assets { get; set; } = [];
    }
}
