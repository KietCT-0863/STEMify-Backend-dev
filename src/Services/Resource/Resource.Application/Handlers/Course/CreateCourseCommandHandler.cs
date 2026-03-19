using Contracts.Abstractions.Services;
using EventBus.Messages.Resource;
using MassTransit;
using MediatR;
using Resource.Application.Commands.Course;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Resource;
using Shared.Protos.User;

namespace Resource.Application.Handlers.Course
{
    public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IUserCacheService _userCache;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateCourseCommandHandler(
            IResourceUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ICloudinaryService cloudinaryService,
            IUserCacheService userCache
        )
        {
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _cloudinaryService = cloudinaryService;
            _userCache = userCache;
        }

        public async Task<CourseResponse> Handle(
            CreateCourseCommand request,
            CancellationToken cancellationToken
        )
        {
            if (await _unitOfWork.Courses.AnyAsync(
                c => c.Code.ToLower() == request.Code.ToLower(),
                cancellationToken))
            {
                throw new ApplicationException($"Course code '{request.Code}' already exists.");
            }

            // Validate the student exists
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
                    FileName = $"{request.Slug}-image",
                };
                imageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }

            var course = new Domain.Entities.Course
            {
                Title = request.Title,
                Code = request.Code,
                ImageUrl = imageUrl,
                Slug = request.Slug,
                Description = request.Description,
                Prerequisites = request.Prerequisites,
                StudentTasks = request.StudentTasks,
                Level = request.Level,
                Status = Domain.Enums.CourseStatus.Draft,
                CreatedByUserId = request.CreatedByUserId,
                AgeRangeId = request.AgeRangeId,
                KitId = request.KitId,
                CurriculumCourses = request.CurriculumIds.Select(cid => new Domain.Entities.CurriculumCourse
                {
                    CurriculumId = cid
                }).ToList(),
            };

            await _unitOfWork.Courses.AddAsync(course, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _publishEndpoint.Publish(
                new CourseCreatedEvent
                {
                    CourseId = course.Id,
                    Title = course.Title,
                    CreatedByUserId = course.CreatedByUserId,
                },
                cancellationToken
            );

            var response = new CourseResponse
            {
                Id = course.Id,
                Code = course.Code,
                Title = course.Title,
                ImageUrl = course.ImageUrl,
                Slug = course.Slug,
                Description = course.Description,
                Prerequisites = course.Prerequisites,
                StudentTasks = course.StudentTasks,
                Duration = course.Duration,
                Status = course.Status.ToString(),
                Level = course.Level.ToString(),
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    course.CreatedDate
                ),
                KitId = course.KitId,
                AgeRangeId = course.AgeRangeId,
            };

            return response;
        }
    }
}
