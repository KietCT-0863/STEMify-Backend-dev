using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Content;
using Resource.Application.Specifications.Contents;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Content
{
    public class QueryContentsQueryHandler : IRequestHandler<QueryContentsQuery, PagedContentList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryContentsQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedContentList> Handle(
            QueryContentsQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new ContentParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                Status = request.Status,
                ContentType = request.ContentType,
                SectionId = request.SectionId,
            };

            var pageRequest = filter.ToPageRequest();

            Expression<Func<Domain.Entities.Content, bool>> predicate = c =>
                (
                    string.IsNullOrEmpty(filter.Search)
                    || c.ContentBody.ToLower().Contains(filter.Search)
                )
                && (!filter.Status.HasValue || c.Status == filter.Status.Value)
                && (!filter.ContentType.HasValue || c.ContentType == filter.ContentType.Value)
                && (!filter.SectionId.HasValue || c.SectionId == filter.SectionId.Value);

            Expression<Func<Domain.Entities.Content, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.ContentBody,
                    _ => c => c.ContentBody,
                };

            var paged = await _unitOfWork.Contents.GetByPageFilter(
                pageRequest,
                sortExpression: sortExpression,
                projectionFunc: c => c.Include(c => c.Quiz).Include(c => c.Assignment),
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new PagedContentList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var content in paged.Items)
            {
                var contentResponse = new ContentResponse
                {
                    Id = content.Id,
                    Status = content.Status.ToString(),
                    ContentBody = content.ContentBody,
                    ContentType = content.ContentType.ToString(),
                    FileName = content.FileName,
                    FileUrl = content.FileUrl ?? "",
                    UploadDate = content.UploadDate.HasValue
                        ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            content.UploadDate.Value
                        )
                        : null,
                    SectionId = content.SectionId,
                };
                if (content.Quiz != null)
                {
                    contentResponse.QuizTitle = content.Quiz.Title;
                    contentResponse.QuizId = content.Quiz.Id;
                    contentResponse.TimeLimitInMinutes = content.Quiz.TimeLimitInMinutes;
                    contentResponse.PassingMarks = (long?)content.Quiz.PassingMarks;
                    contentResponse.TotalMarks = (long?)content.Quiz.TotalMarks;
                    contentResponse.QuizDescription = content.Quiz.Description;
                    contentResponse.DurationDays = content.Quiz.DurationDays;
                }
                else if (content.Assignment != null)
                {
                    contentResponse.AssignmentId = content.Assignment.Id;
                    contentResponse.TotalMarks = (long)content.Assignment.TotalScore;
                    contentResponse.PassingMarks = (long)content.Assignment.PassingScore;
                    if (content.Assignment.DurationDays.HasValue)
                        contentResponse.DurationDays = content.Assignment.DurationDays.Value;
                    contentResponse.AssignmentTitle = content.Assignment.Title;
                }

                response.Items.Add(contentResponse);
            }

            return response;
        }
    }
}
