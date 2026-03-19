using MediatR;
using Resource.Application.Commands.LessonAsset;
using Resource.Application.Common.Interfaces;
using Resource.Domain.Entities;

namespace Resource.Application.Handlers.LessonAsset
{
    public class CreateLessonAssetTagsCommandHandler : IRequestHandler<CreateLessonAssetTagsCommand, bool>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        public CreateLessonAssetTagsCommandHandler(
            IResourceUnitOfWork unitOfWork
        )
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<bool> Handle(CreateLessonAssetTagsCommand request, CancellationToken cancellationToken)
        {
            var lessonAssetTagList = new List<LessonAssetTag>();
            var tagList = new List<Tag>();

            var lessonAsset = await _unitOfWork.LessonAssets.FindByIdAsync(request.LessonAssetId, cancellationToken);
            if (lessonAsset == null)
            {
                throw new KeyNotFoundException($"Lesson Asset with ID {request.LessonAssetId} not found.");
            }

            // Add existing tags by IDs
            if (request.TagIds != null && request.TagIds.Any())
            {
                var existingAssetTagIds = (await _unitOfWork.LessonAssetTags.FindAsync(lt => lt.LessonAssetId == request.LessonAssetId, cancellationToken))
                                            .Select(lt => lt.TagId).ToHashSet();
                var newTagIds = request.TagIds.Where(id => !existingAssetTagIds.Contains(id)).ToList();

                foreach (var tagId in newTagIds)
                {
                    var tag = await _unitOfWork.Tags.FindByIdAsync(tagId, cancellationToken);
                    if (tag != null)
                    {
                        var lessonAssetTag = new LessonAssetTag
                        {
                            LessonAssetId = request.LessonAssetId,
                            TagId = tagId
                        };
                        lessonAssetTagList.Add(lessonAssetTag);
                    }
                }
                await _unitOfWork.LessonAssetTags.AddRangeAsync(lessonAssetTagList, cancellationToken);
            }

            // Add new tags by names
            if (request.TagNames != null && request.TagNames.Any())
            {
                foreach (var tagName in request.TagNames)
                {
                    var tag = new Tag
                    {
                        Name = tagName,
                        LessonAssetTags = new List<LessonAssetTag>
                    {
                        new LessonAssetTag
                        {
                            LessonAssetId = request.LessonAssetId
                        }
                    }
                    };
                    tagList.Add(tag);
                }
                await _unitOfWork.Tags.AddRangeAsync(tagList, cancellationToken);
            }

            return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
