using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Course;
using Resource.Application.Specifications.Courses;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Course
{
    public class GetCourseListQueryHandler : IRequestHandler<GetCourseListQuery, CourseList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetCourseListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseList> Handle(
            GetCourseListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new CourseWithIncludesSpecification();
                var courses = await _unitOfWork.Courses.GetAllAsync(spec, cancellationToken);

                var list = new CourseList();
                foreach (var course in courses)
                {
                    var response = new CourseResponse
                    {
                        Id = course.Id,
                        Title = course.Title,
                        ImageUrl = course.ImageUrl,
                        Slug = course.Slug,
                        Description = course.Description,
                        Duration = course.Lessons?.Sum(c => c.Duration) ?? 0,
                        Status = course.Status.ToString(),
                        AgeRangeId = course.AgeRangeId,
                        CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            course.CreatedDate
                        ),
                        LastModifiedDate =
                            course.LastModifiedDate != null
                                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                                    course.LastModifiedDate.Value
                                )
                                : null,
                        AgeRangeLabel = course.AgeRange?.AgeRangeLabel,
                    };

                    //response.CategoryNames.AddRange(
                    //    course
                    //        .CourseCategories?.Where(cc =>
                    //            cc.Category != null
                    //            && !string.IsNullOrEmpty(cc.Category.CategoryName)
                    //        )
                    //        .Select(cc => cc.Category.CategoryName) ?? Enumerable.Empty<string>()
                    //);

                    //response.SkillNames.AddRange(
                    //    course
                    //        .CourseSkills?.Where(cc =>
                    //            cc.Skill != null && !string.IsNullOrEmpty(cc.Skill.SkillName)
                    //        )
                    //        .Select(cc => cc.Skill.SkillName) ?? Enumerable.Empty<string>()
                    //);

                    //response.StandardNames.AddRange(
                    //    course
                    //        .CourseStandards?.Where(cc =>
                    //            cc.Standard != null
                    //            && !string.IsNullOrEmpty(cc.Standard.StandardName)
                    //        )
                    //        .Select(cc => cc.Standard.StandardName) ?? Enumerable.Empty<string>()
                    //);

                    response.LessonIds.AddRange(
                        course.Lessons?.Select(l => l.Id) ?? Enumerable.Empty<int>()
                    );

                    list.Courses.Add(response);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the course list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
