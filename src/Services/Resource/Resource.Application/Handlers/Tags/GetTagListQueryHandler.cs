using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Tags;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Tags
{
    public class GetTagListQueryHandler : IRequestHandler<GetTagListQuery, PagedTagList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetTagListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<PagedTagList> Handle(GetTagListQuery request, CancellationToken cancellationToken)
        {
            var pageRequest = new PageRequest
            {
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
            };

            Expression<Func<Domain.Entities.Tag, object>>? sortExpression = c => c.Name;

            Expression<Func<Domain.Entities.Tag, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(request.Search)
                        || c.Name.ToLower().Contains(request.Search)
                    );

            var pagedTags = await _unitOfWork.Tags.GetByPageFilter(
                pageRequest: pageRequest,
                sortExpression: e => e.Name,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new PagedTagList
            {
                Items =
                {
                    pagedTags.Items.Select(c => new TagModel
                    {
                        Id = c.Id,
                        Name = c.Name
                    }),
                },
                PageNumber = pagedTags.PageNumber,
                PageSize = pagedTags.PageSize,
                TotalCount = pagedTags.TotalCount,
                TotalPages = pagedTags.TotalPages
            };
            return response;
        }
    }
}
