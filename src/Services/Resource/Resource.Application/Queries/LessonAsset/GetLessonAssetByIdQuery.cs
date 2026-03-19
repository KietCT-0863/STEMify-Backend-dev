using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.LessonAsset
{
    public class GetLessonAssetByIdQuery : IRequest<LessonAssetResponse>
    {
        public int Id { get; set; }

        public GetLessonAssetByIdQuery(int id)
        {
            Id = id;
        }
    }

}
