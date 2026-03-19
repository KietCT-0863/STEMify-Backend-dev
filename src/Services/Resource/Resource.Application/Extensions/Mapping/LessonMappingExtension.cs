using Contracts.Abstractions.Paging;
using Shared.Protos.Resource;

namespace Resource.Application.Extensions.Mapping
{
    public static class LessonMappingExtension
    {
        // Map Domain LessonResponse to gRPC LessonResponse
        public static Shared.Protos.Resource.LessonResponse ToGrpcLessonResponse(
            this Resource.Application.Models.Lesson.LessonResponse model
        )
        {
            var grpcLesson = new Shared.Protos.Resource.LessonResponse
            {
                Id = model.Id,
                Title = model.Title ?? string.Empty,
                ImageUrl = model.ImageUrl ?? string.Empty,
                Description = model.Description ?? string.Empty,
                LearningOutcome = model.LearningOutcome ?? string.Empty,
                Requirement = model.Requirement ?? string.Empty,
                Duration = model.Duration,
                OrderIndex = model.OrderIndex,
                Status = model.Status.ToString(),
                CreatedByUserId = model.CreatedByUserId ?? string.Empty,
                CreatedByUserName = model.CreatedByUserName ?? string.Empty,
                CourseId = model.CourseId,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    model.CreatedDate
                ),
                LastModifiedDate = model.LastModifiedDate.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        model.LastModifiedDate.Value
                    )
                    : null,
                AgeRangeLabel = model.AgeRangeLabel ?? string.Empty,
            };

            grpcLesson.TopicNames.AddRange(model.TopicNames ?? new List<string>());
            grpcLesson.SkillNames.AddRange(model.SkillNames ?? new List<string>());
            grpcLesson.StandardNames.AddRange(model.StandardNames ?? new List<string>());
            grpcLesson.SectionIds.AddRange(model.SectionIds ?? new List<int>());

            return grpcLesson;
        }

        // Map a page of Domain LessonResponse to gRPC PagedLessonList
        public static PagedLessonList ToGrpcPagedLessonList<T>(
            this IPageList<T> paged,
            Func<T, Shared.Protos.Resource.LessonResponse> mapper
        )
            where T : class
        {
            var response = new PagedLessonList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            response.Items.AddRange(paged.Items.Select(mapper));
            return response;
        }
    }
}
