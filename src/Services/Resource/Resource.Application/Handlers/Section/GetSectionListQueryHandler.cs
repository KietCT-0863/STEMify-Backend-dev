using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Section;
using Resource.Application.Specifications.Sections;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Section
{
    public class GetSectionListQueryHandler : IRequestHandler<GetSectionListQuery, SectionList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetSectionListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SectionList> Handle(
            GetSectionListQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new SectionWithIncludesSpecification();
            var sections = await _unitOfWork.Sections.GetAllAsync(spec, cancellationToken);

            var list = new SectionList();
            foreach (var section in sections)
            {
                var response = new SectionResponse
                {
                    Id = section.Id,
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

                list.Sections.Add(response);
            }

            return list;
        }
    }
}
