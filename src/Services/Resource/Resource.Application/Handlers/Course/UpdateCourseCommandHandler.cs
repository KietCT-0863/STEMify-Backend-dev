using Contracts.Abstractions.Services;
using EventBus.Messages.Resource;
using MassTransit;
using MediatR;
using Resource.Application.Commands.Course;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Courses;
using Shared.DTOs.Cloudinary;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Course
{
    public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateCourseCommandHandler(
            IResourceUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IPublishEndpoint publishEndpoint
        )
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<CourseResponse> Handle(
            UpdateCourseCommand request,
            CancellationToken cancellationToken
        )
        {

                var spec = new CourseByIdSpecification(request.Id);
                var course = await _unitOfWork.Courses.FirstOrDefaultAsync(spec, cancellationToken);
                if (course == null)
                    throw new KeyNotFoundException("Course does not exist");
                var currentStatus = course.Status;

                // Check for duplicate code if code is being updated
                if (!string.IsNullOrWhiteSpace(request.Code) &&
                    !string.Equals(course.Code, request.Code, StringComparison.OrdinalIgnoreCase))
                {
                    var codeExists = await _unitOfWork.Courses.AnyAsync(
                        c => c.Id != course.Id && c.Code.ToLower() == request.Code.ToLower(),
                        cancellationToken
                    );
                    if (codeExists)
                    {
                        throw new ApplicationException($"Course code '{request.Code}' already exists.");
                    }
                }

                await UpdateCourseFieldsAsync(course, request, cancellationToken);

                //if (request.Status.HasValue && currentStatus == Domain.Enums.CourseStatus.Pending)
                //{
                //    course.ReviewedAt = DateTime.UtcNow;
                //    // publish event to message queue
                //    var courseUpdated = new CourseUpdatedEvent
                //    {
                //        Id = course.Id,
                //        Title = course.Title,
                //        Status = course.Status.ToString(),
                //        CreatedByUserId = course.CreatedByUserId.ToString(),
                //    };
                //    await _publishEndpoint.Publish(courseUpdated);
                //}

                // Save changes to the database
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var updatedCourse = await _unitOfWork.Courses.FirstOrDefaultAsync(
                    spec,
                    cancellationToken
                );
                return updatedCourse != null ? MapToResponse(updatedCourse) : null;

        }

        private async Task UpdateCourseFieldsAsync(
            Domain.Entities.Course course,
            UpdateCourseCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ImageBytes != null && request.ImageBytes.Length > 0)
            {
                var uploadRequest = new UploadImageBytesRequest
                {
                    FileBytes = request.ImageBytes,
                    FileName = $"{request.Slug}-image",
                };
                course.ImageUrl = (await _cloudinaryService.UploadImageAsync(uploadRequest)).AssetUrl;
            }

            course.Title = string.IsNullOrWhiteSpace(request.Title) ? course.Title : request.Title;
            course.Slug = string.IsNullOrWhiteSpace(request.Slug) ? course.Slug : request.Slug;
            course.Description = string.IsNullOrWhiteSpace(request.Description) ? course.Description : request.Description;
            course.Code = string.IsNullOrWhiteSpace(request.Code) ? course.Code : request.Code;
            course.StudentTasks = string.IsNullOrWhiteSpace(request.StudentTasks) ? course.StudentTasks : request.StudentTasks;
            course.Prerequisites = string.IsNullOrWhiteSpace(request.Prerequisites) ? course.Prerequisites : request.Prerequisites;

            if (request.Level.HasValue)
                course.Level = (Domain.Enums.CourseLevel)(int)request.Level;

            if (request.Status.HasValue)
                course.Status = (Domain.Enums.CourseStatus)(int)request.Status;

            if (request.AgeRangeId.HasValue)
                course.AgeRangeId = request.AgeRangeId.Value;

            if (request.KitId.HasValue && request.KitId.Value == -1)
                course.KitId = null;
            else if (request.KitId.HasValue && request.KitId.Value > 0)
                course.KitId = request.KitId.Value;

            if (request.Status.HasValue)
            {
                course.Status = (Domain.Enums.CourseStatus)request.Status;
                foreach (var lesson in course.Lessons)
                {
                    lesson.Status = (Domain.Enums.LessonStatus)request.Status.Value;
                    foreach (var section in lesson.Sections)
                    {
                        section.Status = (Domain.Enums.SectionStatus)request.Status.Value;
                        foreach (var content in section.Contents)
                        {
                            content.Status = (Domain.Enums.ContentStatus)request.Status.Value;
                        }
                    }
                }
            }

            course.LastModifiedDate = DateTimeOffset.UtcNow;

            if (request.CurriculumIds != null && request.CurriculumIds.Any())
            {
                var curriculumsToRemove = course
                    .CurriculumCourses.Where(cs => !request.CurriculumIds.Contains(cs.CurriculumId))
                    .ToList();

                foreach (var curriculum in curriculumsToRemove)
                    course.CurriculumCourses.Remove(curriculum);

                foreach (var curriculumId in request.CurriculumIds)
                {
                    if (!course.CurriculumCourses.Any(cs => cs.CurriculumId == curriculumId))
                        course.CurriculumCourses.Add(
                            new Domain.Entities.CurriculumCourse { CurriculumId = curriculumId }
                        );
                }
            }

            await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
        }

        //private async Task UpdateLessonStatusesAsync(
        //    int courseId,
        //    Domain.Enums.CourseStatus currentCourseStatus,
        //    CourseStatus newCourseStatus,
        //    CancellationToken cancellationToken)
        //{
        //    Expression<Func<Domain.Entities.Lesson, bool>> predicate = null;
        //    Domain.Enums.LessonStatus newLessonStatus;

        //    if (newCourseStatus == CourseStatus.Pending)
        //    {
        //        predicate = l => l.CourseId == courseId && l.Status == Domain.Enums.LessonStatus.Draft;
        //        newLessonStatus = Domain.Enums.LessonStatus.Pending;
        //    }
        //    else if (newCourseStatus == CourseStatus.Published)
        //    {
        //        predicate = l => l.CourseId == courseId;
        //        newLessonStatus = Domain.Enums.LessonStatus.Published;
        //    }
        //    else
        //    {
        //        return; // no need to update lessons
        //    }

        //    var lessons = await _unitOfWork.Lessons.FindAsync(predicate, cancellationToken);

        //    foreach (var lesson in lessons)
        //    {
        //        lesson.Status = newLessonStatus;
        //        lesson.LastModifiedDate = DateTimeOffset.UtcNow;
        //        await _unitOfWork.Lessons.UpdateAsync(lesson, cancellationToken);
        //    }
        //}

        private CourseResponse MapToResponse(Domain.Entities.Course course)
        {
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
                AgeRangeId = course.AgeRangeId,
                CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(course.CreatedDate),
                LastModifiedDate = course.LastModifiedDate.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(course.LastModifiedDate.Value)
                    : null,
                AgeRangeLabel = course.AgeRange?.AgeRangeLabel,
                KitId = course.KitId,
            };

            response.LessonIds.AddRange(course.Lessons?.Select(l => l.Id) ?? Enumerable.Empty<int>());
            return response;
        }


    }
}
