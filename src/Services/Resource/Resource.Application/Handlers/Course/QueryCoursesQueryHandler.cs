using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Queries.Course;
using Resource.Application.Specifications.Courses;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Course
{
    public class QueryCoursesQueryHandler : IRequestHandler<QueryCoursesQuery, PagedCourseList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IUserCacheService _userCache;

        public QueryCoursesQueryHandler(IResourceUnitOfWork unitOfWork, IUserCacheService userCache)
        {
            _unitOfWork = unitOfWork;
            _userCache = userCache;
        }

        public async Task<PagedCourseList> Handle(
            QueryCoursesQuery request,
            CancellationToken cancellationToken
        )
        {
            var filter = new CourseParams
            {
                Search = request.Search,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                OrderBy = request.OrderBy,
                CreatedByUserId = request.CreatedByUserId,
                Status = request.Status,
                AgeRangeId = request.AgeRangeId,
                CategoryId = request.CategoryId,
                SkillId = request.SkillId,
                StandardId = request.StandardId,
                KitId = request.KitId
            };

            var pageRequest = filter.ToPageRequest();

            Expression<Func<Domain.Entities.Course, bool>> predicate = c =>
                (
                    string.IsNullOrEmpty(filter.Search)
                    || c.Title.ToLower().Contains(filter.Search)
                    || c.Code.ToLower().Contains(filter.Search)
                )
                && (filter.Status.HasValue ? c.Status == filter.Status.Value
                : (c.Status != Domain.Enums.CourseStatus.Deleted && c.Status != Domain.Enums.CourseStatus.Archived))
                && (
                    string.IsNullOrEmpty(filter.CreatedByUserId)
                    || c.CreatedByUserId == filter.CreatedByUserId
                )
                && (!filter.KitId.HasValue || c.KitId == filter.KitId.Value)
                && (!filter.AgeRangeId.HasValue || c.AgeRangeId == filter.AgeRangeId.Value);

            Expression<Func<Domain.Entities.Course, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "title" => c => c.Title,
                    "createddate" => c => c.CreatedDate,
                    "lastmodifieddate" => c => c.LastModifiedDate ?? DateTime.MinValue,
                    "duration" => c => c.Duration,
                    _ => c => c.CreatedDate,
                };

            bool descending = request.SortDirection == Shared.Enums.SortDirection.Desc;

            var paged = await _unitOfWork.Courses.GetByPageFilter(
                pageRequest,
                query =>
                    query
                        .Include(c => c.AgeRange)
                        .Select(course => new Models.Course.CourseResponse
                        {
                            Id = course.Id,
                            Title = course.Title,
                            Code = course.Code,
                            ImageUrl = course.ImageUrl,
                            Slug = course.Slug,
                            Description = course.Description,
                            Prerequisites = course.Prerequisites,
                            StudentTasks = course.StudentTasks,
                            Level = course.Level,
                            Duration = course.Duration,
                            Status = course.Status,
                            CreatedByUserId = course.CreatedByUserId,
                            AgeRangeId = course.AgeRangeId,
                            CreatedDate = course.CreatedDate,
                            LastModifiedDate = course.LastModifiedDate,
                            AgeRangeLabel =
                                course.AgeRange != null
                                    ? course.AgeRange.AgeRangeLabel
                                    : string.Empty,
                            LessonIds =
                                course.Lessons != null
                                    ? course.Lessons.Select(s => s.Id).ToList()
                                    : new List<int>(),
                            KitId = course.KitId,
                        }),
                sortExpression: sortExpression,
                descending: descending,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            return paged.ToGrpcPagedCourseList(x => x.ToGrpcCourseResponse());
        }

    }
}
