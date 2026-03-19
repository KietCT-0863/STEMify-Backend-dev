using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Queries.Course;
using Resource.Application.Specifications.Courses;
using Resource.Domain.Entities;
using Shared.Protos.Classroom;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Course
{
    public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseDetail>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IUserCacheService _userCache;

        public GetCourseByIdQueryHandler(IResourceUnitOfWork unitOfWork, IUserCacheService userCache)
        {
            _unitOfWork = unitOfWork;
            _userCache = userCache;
        }

        public async Task<CourseDetail> Handle(
            GetCourseByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CourseByIdSpecification(request.Id);
            var course = await _unitOfWork.Courses.FirstOrDefaultAsync(spec, cancellationToken);

            if (course == null)
                throw new KeyNotFoundException($"Course with ID {request.Id} not found.");

            var user = await _userCache.GetByIdAsync(Guid.Parse(course.CreatedByUserId), cancellationToken);

            //if (user == null)
            //{
            //    throw new UnauthorizedAccessException(
            //        $"User with ID {course.CreatedByUserId} does not exist."
            //    );
            //}

            var response = new CourseDetail
            {
                Id = course.Id,
                Title = course.Title,
                ImageUrl = course.ImageUrl,
                Slug = course.Slug,
                Description = course.Description,
                Code = course.Code,
                Prerequisites = course.Prerequisites,
                StudentTasks = course.StudentTasks,
                Level = course.Level.ToString(),
                Duration = course.Lessons.Sum(l => l.Duration),
                Status = course.Status.ToString(),
                CreatedByUserId = course.CreatedByUserId.ToString(),
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
                CreatedByUserName = user?.Name ?? course.CreatedByUserId,
                KitId = course.KitId,
            };

            response.SkillNames.AddRange(
                course
                    .Lessons?.SelectMany(lesson =>
                        lesson.LessonSkills ?? Enumerable.Empty<LessonSkill>()
                    )
                    .Where(ls =>
                        ls.Skill != null && !string.IsNullOrWhiteSpace(ls.Skill.SkillName)
                    )
                    .Select(ls => ls.Skill.SkillName)
                    .Distinct() ?? Enumerable.Empty<string>()
            );

            response.TopicNames.AddRange(
                course
                    .Lessons?.SelectMany(lesson =>
                        lesson.LessonTopics ?? Enumerable.Empty<LessonTopic>()
                    )
                    .Where(ls => ls.Topic != null && !string.IsNullOrWhiteSpace(ls.Topic.Name))
                    .Select(ls => ls.Topic.Name)
                    .Distinct() ?? Enumerable.Empty<string>()
            );

            response.StandardNames.AddRange(
                course
                    .Lessons?.SelectMany(lesson =>
                        lesson.LessonStandards ?? Enumerable.Empty<LessonStandard>()
                    )
                    .Where(ls =>
                        ls.Standard != null && !string.IsNullOrWhiteSpace(ls.Standard.Name)
                    )
                    .Select(ls => ls.Standard.Name)
                    .Distinct() ?? Enumerable.Empty<string>()
            );

            response.Lessons.AddRange(
                course.Lessons?.Select(l => new GrpcLessonModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    Duration = l.Duration,
                    SectionIds = { l.Sections.Select(s => s.Id) }
                }) ?? []
            );

            response.CurriculumIds.AddRange(
                course.CurriculumCourses?.Select(cc => cc.CurriculumId) ?? Enumerable.Empty<int>()
            );

            response.QuizIds.AddRange(
                course.Lessons?
                    .SelectMany(l => l.Sections != null ? l.Sections : Enumerable.Empty<Domain.Entities.Section>())
                    .SelectMany(s => s.Contents != null ? s.Contents : Enumerable.Empty<Domain.Entities.Content>())
                    .Where(c => c.Quiz != null && c.Quiz.Id != 0)
                    .Select(c => c.Quiz.Id)
                ?? Enumerable.Empty<int>()
            );

            return response;
        }
    }
}
