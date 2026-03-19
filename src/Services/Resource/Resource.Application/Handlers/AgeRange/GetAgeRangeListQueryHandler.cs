using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.AgeRange;
using Resource.Application.Specifications.AgeRanges;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.AgeRange
{
    public class GetAgeRangeListQueryHandler : IRequestHandler<GetAgeRangeListQuery, AgeRangeList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetAgeRangeListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AgeRangeList> Handle(
            GetAgeRangeListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new AgeRangeWithIncludesSpecification();
                var ageRanges = await _unitOfWork.AgeRanges.GetAllAsync(spec, cancellationToken);

                var ageRangeList = new AgeRangeList();
                foreach (var ageRange in ageRanges)
                {
                    var response = new AgeRangeResponse
                    {
                        Id = ageRange.Id,
                        AgeRangeLabel = ageRange.AgeRangeLabel,
                        MinAge = ageRange.MinAge,
                        MaxAge = ageRange.MaxAge,
                    };
                    response.CourseIds.AddRange(
                        ageRange.Courses?.Select(x => x.Id) ?? Enumerable.Empty<int>()
                    );
                    ageRangeList.AgeRanges.Add(response);
                }

                return ageRangeList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the AgeRange list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
