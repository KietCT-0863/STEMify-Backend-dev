using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.ProgramLearningOutcome;
using Resource.Application.Specifications.ProgramLearningOutcomes;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.ProgramLearningOutcome
{
    public class QueryProgramLearningOutcomesQueryHandler
        : IRequestHandler<QueryProgramLearningOutcomesQuery, PagedProgramLearningOutcomeList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryProgramLearningOutcomesQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedProgramLearningOutcomeList> Handle(
            QueryProgramLearningOutcomesQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new ProgramLearningOutcomeParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                CurriculumId = request.CurriculumId,
            };

            var pageRequest = filter.ToPageRequest();

            Expression<Func<Domain.Entities.ProgramLearningOutcome, bool>> predicate = c =>
                (
                    (string.IsNullOrEmpty(filter.Search) || c.Name.ToLower().Contains(filter.Search))
                    && (!filter.CurriculumId.HasValue || c.CurriculumId == filter.CurriculumId.Value)
                );

            Expression<Func<Domain.Entities.ProgramLearningOutcome, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    _ => c => c.Name,
                };

            var paged = await _unitOfWork.ProgramLearningOutcomes.GetByPageFilter(
                pageRequest,
                sortExpression: sortExpression,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new PagedProgramLearningOutcomeList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var programLearningOutcome in paged.Items)
            {
                var ProgramLearningOutcomeResponse = new ProgramLearningOutcomeResponse
                {
                    Id = programLearningOutcome.Id,
                    Name = programLearningOutcome.Name,
                    Description = programLearningOutcome.Description,
                    CurriculumId = programLearningOutcome.CurriculumId,
                };

                response.Items.Add(ProgramLearningOutcomeResponse);
            }

            return response;
        }
    }
}
