using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Lesson;
using Resource.Application.Specifications.Lessons;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Lesson
{
    public class GetLessonListQueryHandler : IRequestHandler<GetLessonListQuery, LessonList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetLessonListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LessonList> Handle(
            GetLessonListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new LessonWithIncludesSpecification();
                var lessons = await _unitOfWork.Lessons.GetAllAsync(spec, cancellationToken);

                var list = new LessonList();
                foreach (var lesson in lessons)
                {
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
                        AgeRangeLabel = lesson.Course?.AgeRange?.AgeRangeLabel,
                    };

                    response.SectionIds.AddRange(
                        lesson.Sections?.Select(x => x.Id) ?? Enumerable.Empty<int>()
                    );

                    list.Lessons.Add(response);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the lesson list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
