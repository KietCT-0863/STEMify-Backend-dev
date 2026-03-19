using Contracts.Abstractions.Services;
using EventBus.Messages.Resource;
using MassTransit;
using MediatR;
using Resource.Application.Commands.Curriculum;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Curriculums;
using Shared.DTOs.Cloudinary;
using Shared.Exceptions;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Curriculum
{
    public class UpdateCurriculumCommandHandler : IRequestHandler<UpdateCurriculumCommand, CurriculumResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateCurriculumCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IPublishEndpoint publishEndpoint
        )
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<CurriculumResponse> Handle(
            UpdateCurriculumCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CurriculumByIdSpecification(request.Id);
            var curriculum = await _unitOfWork.Curriculums.FirstOrDefaultAsync(spec, cancellationToken);
            if (curriculum == null)
                throw new NotFoundException("Curriculum not found");
            var currentStatus = curriculum.Status;

            // Check for duplicate code if code is being updated
            if (!string.IsNullOrWhiteSpace(request.Code) &&
                !string.Equals(curriculum.Code, request.Code, StringComparison.OrdinalIgnoreCase))
            {
                var codeExists = await _unitOfWork.Curriculums.AnyAsync(
                    c => c.Id != curriculum.Id && c.Code.ToLower() == request.Code.ToLower(),
                    cancellationToken
                );
                if (codeExists)
                {
                    throw new ApplicationException($"Curriculum code '{request.Code}' already exists.");
                }
            }

            await UpdateCurriculumFieldsAsync(curriculum, request, cancellationToken);

            // Save changes to the database
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedCurriculum = await _unitOfWork.Curriculums.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );
            return updatedCurriculum != null ? MapToResponse(updatedCurriculum) : null;
        }

        private async Task UpdateCurriculumFieldsAsync(
            Domain.Entities.Curriculum curriculum,
            UpdateCurriculumCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Title}-image",
                };
                curriculum.ImageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }

            curriculum.Title = string.IsNullOrWhiteSpace(request.Title) ? curriculum.Title : request.Title;
            curriculum.Description = string.IsNullOrWhiteSpace(request.Description) ? curriculum.Description : request.Description;
            curriculum.Code = string.IsNullOrWhiteSpace(request.Code) ? curriculum.Code : request.Code;

            if (request.Status.HasValue)
                curriculum.Status = (Domain.Enums.CurriculumStatus)(int)request.Status;

            //curriculum.LastModifiedDate = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
            curriculum.LastModifiedDate = DateTimeOffset.UtcNow;

            await _unitOfWork.Curriculums.UpdateAsync(curriculum, cancellationToken);
        }

        private CurriculumResponse MapToResponse(Domain.Entities.Curriculum curriculum)
        {
            var response = new CurriculumResponse
            {
                Id = curriculum.Id,
                Code = curriculum.Code,
                Title = curriculum.Title,
                ImageUrl = curriculum.ImageUrl,
                Description = curriculum.Description,
                Status = curriculum.Status.ToString(),
                CreatedByUserId = curriculum.CreatedByUserId.ToString(),
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(curriculum.CreatedDate),
                LastModifiedDate = curriculum.LastModifiedDate.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(curriculum.LastModifiedDate.Value)
                    : null,
                ApprovedByUserId = curriculum.ApprovedByUserId?.ToString(),
                ApprovedAt = curriculum.ApprovedAt.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(curriculum.ApprovedAt.Value)
                    : null,
            };

            return response;
        }
    }
}
