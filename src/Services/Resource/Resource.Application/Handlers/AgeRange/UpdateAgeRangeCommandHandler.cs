using MediatR;
using Resource.Application.Commands.AgeRange;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.AgeRange
{
    public class UpdateAgeRangeCommandHandler
        : IRequestHandler<UpdateAgeRangeCommand, AgeRangeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateAgeRangeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AgeRangeResponse> Handle(
            UpdateAgeRangeCommand request,
            CancellationToken cancellationToken
        )
        {
            var ageRange = await _unitOfWork.AgeRanges.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (ageRange == null)
                throw new KeyNotFoundException($"AgeRange with ID {request.Id} not found.");

            ageRange.AgeRangeLabel = request.AgeRangeLabel;
            ageRange.MaxAge = request.MaxAge;
            ageRange.MinAge = request.MinAge;

            await _unitOfWork.AgeRanges.UpdateAsync(ageRange, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
