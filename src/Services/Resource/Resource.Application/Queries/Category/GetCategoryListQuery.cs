using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Category
{
    public class GetCategoryListQuery : IRequest<CategoryList> { }
}
