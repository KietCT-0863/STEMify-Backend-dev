using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Curriculum;
using Resource.Application.Specifications.Curriculums;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Curriculum
{
    public class GetCurriculumListQueryHandler : IRequestHandler<GetCurriculumListQuery, CurriculumList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetCurriculumListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CurriculumList> Handle(
            GetCurriculumListQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CurriculumWithIncludesSpecification();
            var curriculums = await _unitOfWork.Curriculums.GetAllAsync(spec, cancellationToken);

            var list = new CurriculumList();
            foreach (var curriculum in curriculums)
            {
                var response = new CurriculumResponse
                {
                    Id = curriculum.Id,
                    Title = curriculum.Title,
                    ImageUrl = curriculum.ImageUrl,
                    Description = curriculum.Description,
                    Status = curriculum.Status.ToString(),
                    CourseCount = curriculum.CurriculumCourses.Count,
                    Duration = curriculum.CurriculumCourses.Sum(cc => cc.Course?.Duration ?? 0),
                    CreatedByUserId = curriculum.CreatedByUserId.ToString(),
                    CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                        curriculum.CreatedDate
                    ),
                    LastModifiedDate =
                        curriculum.LastModifiedDate != null
                            ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                                curriculum.LastModifiedDate.Value
                            )
                            : null,
                };
                list.Curriculums.Add(response);
            }

            return list;
        }
    }
}
