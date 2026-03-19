using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Content;
using Resource.Application.Specifications.Contents;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Content
{
    public class GetContentByIdQueryHandler : IRequestHandler<GetContentByIdQuery, ContentResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetContentByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ContentResponse> Handle(
            GetContentByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new ContentByIdSpecification(request.Id);

            var content = await _unitOfWork.Contents.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (content == null)
                throw new KeyNotFoundException($"Content with ID {request.Id} not found.");

            var response = new ContentResponse
            {
                Id = content.Id,
                ContentBody = content.ContentBody,
                ContentType = content.ContentType.ToString(),
                FileName = content.FileName,
                FileUrl = content.FileUrl,
                UploadDate = content.UploadDate.HasValue
                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        content.UploadDate.Value
                    )
                    : null,
                Status = content.Status.ToString(),
                SectionId = content.SectionId,
            };

            if (content.Quiz != null)
            {
                response.QuizTitle = content.Quiz.Title;
                response.QuizDescription = content.Quiz.Description;
                response.TotalMarks = (long)content.Quiz.TotalMarks;
                response.PassingMarks = (long)content.Quiz.PassingMarks;
                if (content.Quiz.TimeLimitInMinutes.HasValue)
                    response.TimeLimitInMinutes = content.Quiz.TimeLimitInMinutes.Value;
                response.QuizId = content.Quiz.Id;
                response.DurationDays = content.Quiz.DurationDays;
            }
            else if (content.Assignment != null)
            {
                response.AssignmentId = content.Assignment.Id;
                response.TotalMarks = (long)content.Assignment.TotalScore;
                response.PassingMarks = (long)content.Assignment.PassingScore;
                if (content.Assignment.DurationDays.HasValue)
                    response.DurationDays = content.Assignment.DurationDays.Value;
            }

            return response;
        }
    }
}