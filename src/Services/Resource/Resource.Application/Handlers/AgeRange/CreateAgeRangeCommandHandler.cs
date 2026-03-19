using MediatR;
using Resource.Application.Commands.AgeRange;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.AgeRange
{
    public class CreateAgeRangeCommandHandler
        : IRequestHandler<CreateAgeRangeCommand, AgeRangeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateAgeRangeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AgeRangeResponse> Handle(
            CreateAgeRangeCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var ageRange = new Domain.Entities.AgeRange
                {
                    AgeRangeLabel = request.AgeRangeLabel,
                    MinAge = request.MinAge,
                    MaxAge = request.MaxAge,
                };

                await _unitOfWork.AgeRanges.AddAsync(ageRange, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new AgeRangeResponse()
                {
                    Id = ageRange.Id,
                    AgeRangeLabel = ageRange.AgeRangeLabel,
                    MinAge = ageRange.MinAge,
                    MaxAge = ageRange.MaxAge,
                };
                response.CourseIds.AddRange(
                    ageRange.Courses?.Select(x => x.Id) ?? Enumerable.Empty<int>()
                );

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while creating the AgeRange: {ex.Message}",
                    ex
                );
            }
        }
    }
}
