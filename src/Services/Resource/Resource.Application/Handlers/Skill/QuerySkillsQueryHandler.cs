using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Skill;
using Resource.Application.Specifications.Skills;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Skill
{
    public class QuerySkillsQueryHandler : IRequestHandler<QuerySkillsQuery, PagedSkillList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QuerySkillsQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedSkillList> Handle(
            QuerySkillsQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var filter = new SkillParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.Skill, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.SkillName.ToLower().Contains(filter.Search)
                    );

                Expression<Func<Domain.Entities.Skill, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "name" => c => c.SkillName,
                        _ => c => c.SkillName,
                    };

                var paged = await _unitOfWork.Skills.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedSkillList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var Skill in paged.Items)
                {
                    var SkillResponse = new SkillResponse
                    {
                        Id = Skill.Id,
                        SkillName = Skill.SkillName,
                    };

                    response.Items.Add(SkillResponse);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the Skill list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
