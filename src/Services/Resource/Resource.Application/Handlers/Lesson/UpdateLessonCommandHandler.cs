using Contracts.Abstractions.Services;
using MediatR;
using Resource.Application.Commands.Lesson;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Lessons;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Lesson
{
    public class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand, LessonResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;

        public UpdateLessonCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService
        )
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<LessonResponse> Handle(
            UpdateLessonCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new LessonDetailByIdSpecification(request.Id);
            var lesson = await _unitOfWork.Lessons.FirstOrDefaultAsync(spec, cancellationToken);
            if (lesson == null)
                throw new KeyNotFoundException($"Lesson with ID {request.Id} not found.");

            string imageUrl = lesson.ImageUrl;
            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Title}-image",
                };
                imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }

            if (!string.IsNullOrEmpty(request.Description))
                lesson.Description = request.Description;
            if (request.Status.HasValue)
            {
                lesson.Status = request.Status.Value;
                if (lesson.Sections != null)
                {
                    foreach (var section in lesson.Sections)
                    {
                        section.Status = (Domain.Enums.SectionStatus)request.Status.Value;
                        if (section.Contents != null)
                        {
                            foreach (var content in section.Contents)
                            {
                                content.Status = (Domain.Enums.ContentStatus)request.Status.Value;
                            }
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(request.Title))
                lesson.Title = request.Title;
            if (!string.IsNullOrEmpty(request.LearningOutcome))
                lesson.LearningOutcome = request.LearningOutcome;
            if (!string.IsNullOrEmpty(request.Requirement))
                lesson.Requirement = request.Requirement;
            if (request.OrderIndex.HasValue)
                lesson.OrderIndex = request.OrderIndex.Value;
            if (request.Duration.HasValue)
                lesson.Duration = request.Duration.Value;
            lesson.ImageUrl = imageUrl;
            lesson.LastModifiedDate = DateTimeOffset.UtcNow;

            // Update Lesson Skills
            if (request.SkillIds != null && request.SkillIds.Any())
            {
                var skillsToRemove = lesson
                    .LessonSkills.Where(cs => !request.SkillIds.Contains(cs.SkillId))
                    .ToList();

                foreach (var skill in skillsToRemove)
                    lesson.LessonSkills.Remove(skill);

                foreach (var skillId in request.SkillIds)
                {
                    if (!lesson.LessonSkills.Any(cs => cs.SkillId == skillId))
                        lesson.LessonSkills.Add(
                            new Domain.Entities.LessonSkill { SkillId = skillId }
                        );
                }
            }

            // --- Update LessonTopics ---
            if (request.TopicIds != null && request.TopicIds.Any())
            {
                var topicsToRemove = lesson
                    .LessonTopics.Where(cc => !request.TopicIds.Contains(cc.TopicId))
                    .ToList();

                foreach (var topic in topicsToRemove)
                    lesson.LessonTopics.Remove(topic);

                foreach (var topicId in request.TopicIds)
                {
                    if (!lesson.LessonTopics.Any(cc => cc.TopicId == topicId))
                        lesson.LessonTopics.Add(
                            new Domain.Entities.LessonTopic { TopicId = topicId }
                        );
                }
            }

            // --- Update LessonStandards ---
            if (request.StandardIds != null && request.StandardIds.Any())
            {
                var standardsToRemove = lesson
                    .LessonStandards.Where(cs => !request.StandardIds.Contains(cs.StandardId))
                    .ToList();

                foreach (var standard in standardsToRemove)
                    lesson.LessonStandards.Remove(standard);

                foreach (var standardId in request.StandardIds)
                {
                    if (!lesson.LessonStandards.Any(cs => cs.StandardId == standardId))
                        lesson.LessonStandards.Add(
                            new Domain.Entities.LessonStandard { StandardId = standardId }
                        );
                }
            }

            await _unitOfWork.Lessons.UpdateAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var lessons = await _unitOfWork.Lessons.GetAllAsync(
                new PublishedLessonsByCourseIdSpecification(lesson.CourseId),
                cancellationToken
            );
            int duration = 0;
            foreach (var l in lessons)
            {
                duration += l.Duration;
            }

            var course = await _unitOfWork.Courses.FindByIdForUpdateAsync(lesson.CourseId, cancellationToken);
            if (course != null)
            {
                course.Duration = duration;
                await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var response = new LessonResponse
            {
                Id = lesson.Id,
                Description = lesson.Description,
                LearningOutcome = lesson.LearningOutcome,
                Requirement = lesson.Requirement,
                Duration = lesson.Duration,
                Status = lesson.Status.ToString(),
                OrderIndex = lesson.OrderIndex,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                ImageUrl = lesson.ImageUrl,
                CreatedByUserId = lesson.CreatedByUserId.ToString(),
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

            return response;
        }
    }
}
