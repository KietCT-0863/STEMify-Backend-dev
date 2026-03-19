using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Standard;
using Resource.Application.Specifications.Standards;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Standard
{
    public class GetStandardByIdQueryHandler
        : IRequestHandler<GetStandardByIdQuery, StandardResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetStandardByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StandardResponse> Handle(
            GetStandardByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new StandardByIdSpecification(request.Id);
            var standard = await _unitOfWork.Standards.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (standard == null)
                throw new KeyNotFoundException($"Standard with ID {request.Id} not found.");

            var response = new StandardResponse()
            {
                Id = standard.Id,
                Description = standard.Description,
                StandardName = standard.Name,
            };

            return response;
        }
    }
}
