using Contracts.Abstractions.Services;
using MediatR;
using Resource.Application.Commands.Lesson;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Resource.Application.Specifications.Lessons;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Resource;
using Shared.Protos.User;

namespace Resource.Application.Handlers.Lesson
{
    public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, LessonResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IUserCacheService _userCache;

        public CreateLessonCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IUserCacheService userCache
        )
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _userCache = userCache;
        }

        public async Task<LessonResponse> Handle(
            CreateLessonCommand request,
            CancellationToken cancellationToken
        )
        {
            var user = await _userCache.GetByIdAsync(Guid.Parse(request.CreatedByUserId), cancellationToken);

            if (user == null)
            {
                throw new UnauthorizedAccessException(
                    $"User with ID {request.CreatedByUserId} does not exist."
                );
            }

            if (user.Role.Equals(UserRole.Student.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    $"User with ID {request.CreatedByUserId} is not authorized to create courses."
                );
            }

            string imageUrl = "";
            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Title}-image",
                };
                imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }

            var spec = new LessonsByCourseIdSpecification(request.CourseId);
            var lessonCount = await _unitOfWork.Lessons.CountAsync(spec, cancellationToken);

            var lesson = new Domain.Entities.Lesson
            {
                Title = request.Title,
                ImageUrl = imageUrl,
                Description = request.Description,
                LearningOutcome = request.LearningOutcome,
                Requirement = request.Requirement,
                OrderIndex = lessonCount,
                CreatedByUserId = request.CreatedByUserId,
                CourseId = request.CourseId,
                LessonSkills = request
                    .SkillIds.Select(skillId => new Domain.Entities.LessonSkill
                    {
                        SkillId = skillId,
                    })
                    .ToList(),
                LessonStandards = request
                    .StandardIds.Select(standardId => new Domain.Entities.LessonStandard
                    {
                        StandardId = standardId,
                    })
                    .ToList(),
                LessonTopics = request
                    .TopicIds.Select(topicId => new Domain.Entities.LessonTopic
                    {
                        TopicId = topicId,
                    })
                    .ToList(),
            };

            await _unitOfWork.Lessons.AddAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new LessonResponse
            {
                Id = lesson.Id,
                Description = lesson.Description,
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
                CreatedByUserName = user.Name,
            };

            return response;
        }
    }
}
