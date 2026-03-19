using Contracts.Abstractions.Services;
using EventBus.Messages.Resource;
using MassTransit;
using MediatR;
using Resource.Application.Commands.Curriculum;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Cache;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Resource;
using Shared.Protos.User;

namespace Resource.Application.Handlers.Curriculum
{
    public class CreateCurriculumCommandHandler : IRequestHandler<CreateCurriculumCommand, CurriculumResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IUserCacheService _userCache;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateCurriculumCommandHandler(
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

        public async Task<CurriculumResponse> Handle(
            CreateCurriculumCommand request,
            CancellationToken cancellationToken
        )
        {
            if (await _unitOfWork.Curriculums.AnyAsync(
                c => c.Code.ToLower() == request.Code.ToLower(),
                cancellationToken))
            {
                throw new ApplicationException($"Curriculum code '{request.Code}' already exists.");
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
                    $"User with ID {request.CreatedByUserId} is not authorized to create curriculums."
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

            var curriculum = new Domain.Entities.Curriculum
            {
                Title = request.Title,
                Code = request.Code,
                ImageUrl = imageUrl,
                Description = request.Description,
                Status = Domain.Enums.CurriculumStatus.Draft,
                CreatedByUserId = request.CreatedByUserId,
            };

            await _unitOfWork.Curriculums.AddAsync(curriculum, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _publishEndpoint.Publish(
                new CurriculumCreatedEvent
                {
                    CurriculumId = curriculum.Id,
                    Title = curriculum.Title,
                    CreatedByUserId = curriculum.CreatedByUserId,
                },
                cancellationToken
            );

            var response = new CurriculumResponse
            {
                Id = curriculum.Id,
                Code = curriculum.Code,
                Title = curriculum.Title,
                ImageUrl = curriculum.ImageUrl,
                Description = curriculum.Description,
                Status = curriculum.Status.ToString(),
                CreatedByUserId = curriculum.CreatedByUserId.ToString(),
                ApprovedByUserId = curriculum.ApprovedByUserId?.ToString(),
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                    curriculum.CreatedDate
                ),

                ApprovedAt =
                    curriculum.ApprovedAt != null
                        ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            curriculum.ApprovedAt.Value
                        )
                        : null,
                CreatedByUserName = user.Name,
            };

            return response;
        }
    }
}
