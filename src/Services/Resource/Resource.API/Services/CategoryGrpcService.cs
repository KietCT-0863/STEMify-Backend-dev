using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Category;
using Resource.Application.Queries.Category;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class CategoryGrpcService : CategoryService.CategoryServiceBase
    {
        private readonly IMediator _mediator;

        public CategoryGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<CategoryResponse> CreateCategory(
            CreateCategoryRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateTopicCommand { Name = request.Name };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<CategoryResponse> GetCategory(
            GetCategoryRequest request,
            ServerCallContext context
        )
        {
            var query = new GetCategoryByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Category with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<CategoryResponse> UpdateCategory(
            UpdateCategoryRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCategoryCommand { Id = request.Id, Name = request.Name };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteCategory(
            DeleteCategoryRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteCategoryCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<CategoryList> ListCategories(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetCategoryListQuery());

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListCategorys failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedCategoryList> QueryCategories(
            QueryCategoriesRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new QueryCategoriesQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OrderBy = request.OrderBy,
                };
                var result = await _mediator.Send(query);

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"SearchCategories failed: {ex.Message}")
                );
            }
        }
    }
}
