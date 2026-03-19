using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Standard;
using Resource.Application.Specifications.Standards;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Standard
{
    public class GetStandardListQueryHandler : IRequestHandler<GetStandardListQuery, StandardList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetStandardListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<StandardList> Handle(
            GetStandardListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new StandardWithIncludesSpecification();
                var standards = await _unitOfWork.Standards.GetAllAsync(spec, cancellationToken);

                var standardList = new StandardList();
                foreach (var standard in standards)
                {
                    var response = new StandardResponse
                    {
                        Id = standard.Id,
                        StandardName = standard.Name,
                        Description = standard.Description
                    };
                    standardList.Standards.Add(response);
                }

                return standardList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the Standard list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
