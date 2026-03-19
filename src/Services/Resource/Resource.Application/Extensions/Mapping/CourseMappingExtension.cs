using Contracts.Abstractions.Paging;
using Shared.Protos.Resource;

namespace Resource.Application.Extensions.Mapping
{
    public static class CourseMappingExtension
    {
        // Map Domain CourseResponse to gRPC CourseResponse
        public static CourseResponse ToGrpcCourseResponse(
            this Models.Course.CourseResponse model
        )
        {
            var grpcCourse = new CourseResponse
            {
                Id = model.Id,
                Code = model.Code,
                Title = model.Title ?? string.Empty,
                ImageUrl = model.ImageUrl ?? string.Empty,
                Slug = model.Slug ?? string.Empty,
                Description = model.Description ?? string.Empty,
                Prerequisites = model.Prerequisites,
                StudentTasks = model.StudentTasks,
                Duration = model.Duration,
                Status = model.Status.ToString(),
                Level = model.Level.ToString(),
                AgeRangeId = model.AgeRangeId,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    model.CreatedDate
                ),
                AgeRangeLabel = model.AgeRangeLabel ?? string.Empty,
                KitId = model.KitId,
            };

            if (model.LastModifiedDate != null)
                grpcCourse.LastModifiedDate =
                    Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        model.LastModifiedDate.Value
                    );
            grpcCourse.TopicNames.AddRange(model.TopicNames ?? new List<string>());
            grpcCourse.SkillNames.AddRange(model.SkillNames ?? new List<string>());
            grpcCourse.StandardNames.AddRange(model.StandardNames ?? new List<string>());
            grpcCourse.LessonIds.AddRange(model.LessonIds ?? new List<int>());

            return grpcCourse;
        }

        // Map a page of Domain CourseResponse to gRPC PagedCourseList
        public static PagedCourseList ToGrpcPagedCourseList<T>(
            this IPageList<T> paged,
            Func<T, CourseResponse> mapper
        )
            where T : class
        {
            var response = new PagedCourseList
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
