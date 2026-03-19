using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Section;
using Resource.Application.Specifications.Sections;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Section
{
    public class GetSectionByIdQueryHandler : IRequestHandler<GetSectionByIdQuery, SectionResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetSectionByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SectionResponse> Handle(
            GetSectionByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new SectionByIdSpecification(request.Id);
            var section = await _unitOfWork.Sections.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (section == null)
                throw new KeyNotFoundException($"Section with ID {request.Id} not found.");

            var response = new SectionResponse
            {
                Id = section.Id,
                Title = section.Title,
                Description = section.Description,
                Duration = section.Duration,
                Status = section.Status.ToString(),
                OrderIndex = section.OrderIndex,
                LessonId = section.LessonId,
                IsVisibleToStudent = section.IsVisibleToStudent
            };

            response.ContentIds.AddRange(
                section.Contents?.Select(x => x.Id) ?? Enumerable.Empty<int>()
            );

            return response;

        }
    }
}
