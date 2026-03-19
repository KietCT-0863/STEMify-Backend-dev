using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.AgeRange;
using Resource.Application.Specifications.AgeRanges;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.AgeRange
{
    public class GetAgeRangeByIdQueryHandler
        : IRequestHandler<GetAgeRangeByIdQuery, AgeRangeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetAgeRangeByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AgeRangeResponse> Handle(
            GetAgeRangeByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new AgeRangeByIdSpecification(request.Id);
            var ageRange = await _unitOfWork.AgeRanges.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );
            if (ageRange == null)
                throw new KeyNotFoundException($"AgeRange with ID {request.Id} not found.");

            var response = new AgeRangeResponse()
            {
                Id = ageRange.Id,
                AgeRangeLabel = ageRange.AgeRangeLabel,
                MaxAge = ageRange.MaxAge,
                MinAge = ageRange.MinAge,
            };
            response.CourseIds.AddRange(
                ageRange.Courses?.Select(x => x.Id) ?? Enumerable.Empty<int>()
            );

            return response;
        }
    }
}
