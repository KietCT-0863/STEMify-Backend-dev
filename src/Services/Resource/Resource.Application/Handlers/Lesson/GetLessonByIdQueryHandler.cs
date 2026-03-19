using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Lesson;
using Resource.Application.Specifications.Lessons;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Lesson
{
    public class GetLessonByIdQueryHandler : IRequestHandler<GetLessonByIdQuery, LessonResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetLessonByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LessonResponse> Handle(
            GetLessonByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new LessonByIdSpecification(request.Id);
            var lesson = await _unitOfWork.Lessons.FirstOrDefaultAsync(spec, cancellationToken);

            if (lesson == null)
                throw new KeyNotFoundException($"Lesson with ID {request.Id} not found.");

            var response = new LessonResponse
            {
                Id = lesson.Id,
                Description = lesson.Description,
                LearningOutcome = lesson.LearningOutcome,
                Requirement = lesson.Requirement,
                Duration = lesson.Duration,
                Status = lesson.Status.ToString(),
                OrderIndex = lesson.OrderIndex,
                Title = lesson.Title,
                ImageUrl = lesson.ImageUrl,
                CreatedByUserId = lesson.CreatedByUserId.ToString(),
                CourseId = lesson.CourseId,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    lesson.CreatedDate
                ),
                LastModifiedDate =
                    lesson.LastModifiedDate != null
                        ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            lesson.LastModifiedDate.Value
                        )
                        : null,
                AgeRangeLabel = lesson.Course?.AgeRange?.AgeRangeLabel,
            };
            response.SectionIds.AddRange(
                lesson.Sections?.Select(x => x.Id) ?? Enumerable.Empty<int>()
            );

            response.SkillNames.AddRange(
                lesson
                    .LessonSkills?.Where(cc =>
                        cc.Skill != null && !string.IsNullOrEmpty(cc.Skill.SkillName)
                    )
                    .Select(cc => cc.Skill.SkillName) ?? Enumerable.Empty<string>()
            );
            response.TopicNames.AddRange(
                lesson
                    .LessonTopics?.Where(cc =>
                        cc.Topic != null && !string.IsNullOrEmpty(cc.Topic.Name)
                    )
                    .Select(cc => cc.Topic.Name) ?? Enumerable.Empty<string>()
            );
            response.StandardNames.AddRange(
                lesson
                    .LessonStandards?.Where(cc =>
                        cc.Standard != null && !string.IsNullOrEmpty(cc.Standard.Name)
                    )
                    .Select(cc => cc.Standard.Name) ?? Enumerable.Empty<string>()
            );

            return response;
        }
    }
}
