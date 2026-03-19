using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Queries.Lesson;
using Resource.Application.Specifications.Lessons;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Lesson
{
    public class QueryLessonsQueryHandler : IRequestHandler<QueryLessonsQuery, PagedLessonList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IUserCacheService _userCache;

        public QueryLessonsQueryHandler(IResourceUnitOfWork unitOfWork, IUserCacheService userCache)
        {
            _unitOfWork = unitOfWork;
            _userCache = userCache;
        }

        public async Task<PagedLessonList> Handle(
            QueryLessonsQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new LessonParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                Status = request.Status,
                CourseId = request.CourseId,
                CreatedByUserId = request.CreatedByUserId,
                Duration = request.Duration,
                AgeRangeId = request.AgeRangeId,
                TopicId = request.TopicId,
                SkillId = request.SkillId,
                StandardId = request.StandardId,
            };

            var pageRequest = filter.ToPageRequest();

            Expression<Func<Domain.Entities.Lesson, bool>> predicate = c =>
                (
                    string.IsNullOrEmpty(filter.Search)
                    || c.Description.ToLower().Contains(filter.Search)
                    || c.Title.ToLower().Contains(filter.Search)
                )
                && (filter.Status.HasValue ? c.Status == filter.Status.Value
                : (c.Status != Domain.Enums.LessonStatus.Deleted && c.Status != Domain.Enums.LessonStatus.Archived))
                && (!filter.CourseId.HasValue || c.CourseId == filter.CourseId.Value)
                && (!filter.Duration.HasValue || c.Duration <= filter.Duration.Value)
                && (
                    string.IsNullOrEmpty(filter.CreatedByUserId)
                    || c.CreatedByUserId == filter.CreatedByUserId
                )
                && (
                    !filter.AgeRangeId.HasValue
                    || c.Course.AgeRangeId == filter.AgeRangeId.Value
                );

            Expression<Func<Domain.Entities.Lesson, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "orderindex" => c => c.OrderIndex,
                    "duration" => c => c.Duration,
                    "title" => c => c.Title,
                    "createddate" => c => c.CreatedDate,
                    "lastmodifieddate" => c => c.LastModifiedDate ?? DateTime.MinValue,
                    _ => c => c.OrderIndex,
                };

            bool descending = request.SortDirection == Shared.Enums.SortDirection.Desc;

            var paged = await _unitOfWork.Lessons.GetByPageFilter(
                pageRequest,
                query =>
                    query
                        .Include(c => c.Sections)
                        .Include(c => c.Course)
                        .ThenInclude(c => c.AgeRange)
                        .Include(c => c.Course)
                        .Select(lesson => new Models.Lesson.LessonResponse
                        {
                            Id = lesson.Id,
                            Title = lesson.Title,
                            ImageUrl = lesson.ImageUrl,
                            Description = lesson.Description,
                            LearningOutcome = lesson.LearningOutcome,
                            Requirement = lesson.Requirement,
                            Duration = lesson.Duration,
                            OrderIndex = lesson.OrderIndex,
                            Status = lesson.Status,
                            CreatedByUserId = lesson.CreatedByUserId,
                            CourseId = lesson.CourseId,
                            CreatedDate = lesson.CreatedDate,
                            LastModifiedDate = lesson.LastModifiedDate,
                            SectionIds =
                                lesson.Sections != null
                                    ? lesson.Sections.Select(s => s.Id).ToList()
                                    : new List<int>(),
                            AgeRangeLabel =
                                lesson.Course.AgeRange != null
                                    ? lesson.Course.AgeRange.AgeRangeLabel
                                    : string.Empty,
                        }),
                sortExpression: sortExpression,
                descending: descending,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            foreach (var lesson in paged.Items)
            {
                var user = await _userCache.GetByIdAsync(Guid.Parse(lesson.CreatedByUserId), cancellationToken);
                lesson.CreatedByUserName = user?.Name ?? lesson.CreatedByUserId;
            }

            return paged.ToGrpcPagedLessonList(x => x.ToGrpcLessonResponse());
        }
    }
}
