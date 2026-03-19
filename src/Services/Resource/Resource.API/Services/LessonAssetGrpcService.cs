using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.LessonAsset;
using Resource.Application.Models.Lesson;
using Resource.Application.Queries.LessonAsset;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class LessonAssetGrpcService : LessonAssetService.LessonAssetServiceBase
    {
        private readonly IMediator _mediator;

        public LessonAssetGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async override Task<CreateLessonAssetsResponse> CreateLessonAssets(
            CreateLessonAssetsRequets request, ServerCallContext context)
        {
            var command = new CreateLessonAssetsCommand
            {
                Assets = request.LessonAssets.Select(a => new CreateLessonAssetRequest
                {
                    Name = a.Name,
                    AssetBytes = a.AssetBytes.ToByteArray(),
                    LessonId = request.LessonId,
                }).ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public async override Task<PagedLessonAssetList> GetLessonAssetList(LessonAssetParams request, ServerCallContext context)
        {
            var query = new GetLessonAssetListQuery
            {
                LessonId = request.LessonId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
                TagId = request.TagId,
                Type = request.Type,
            };
            var result = await _mediator.Send(query);
            return result;
        }

        public async override Task<LessonAssetResponse> GetLessonAssetById(GetLessonAssetByIdRequest request, ServerCallContext context)
        {
            var query = new GetLessonAssetByIdQuery(request.Id);
            var result = await _mediator.Send(query);
            return result;
        }

        public async override Task<Empty> DeleteLessonAsset(DeleteLessonAssetRequest request, ServerCallContext context)
        {
            var query = new DeleteLessonAssetsCommand
            {
                DeletedAssetIds = request.Ids.ToList()
            };
            var result = await _mediator.Send(query);
            return new Empty();
        }

        public async override Task<Empty> CreateLessonAssetTags(
            CreateLessonAssetTagsRequet request, ServerCallContext context)
        {
            var command = new CreateLessonAssetTagsCommand
            {
                LessonAssetId = request.LessonAssetId,
                TagIds = request.TagIds.ToList(),
                TagNames = request.TagNames.ToList(),
            };

            var result = await _mediator.Send(command);
            return new Empty();
        }

        public async override Task<Empty> DeleteLessonAssetTags(
            DeleteLessonAssetTagsRequet request, ServerCallContext context)
        {
            var command = new DeleteLessonAssetTagsCommand
            {
                LessonAssetId = request.LessonAssetId,
                TagIds = request.TagIds.ToList(),
            };
            var result = await _mediator.Send(command);
            return new Empty();
        }
    }
}
