using Infrastructure.Common.Paging;
using MediatR;
using Order.Application.Common.Interfaces;
using Order.Domain.Entities;
using Shared.Protos.Order;
using System.Linq.Expressions;

namespace Order.Application.Queries.OrganizationTypes.GetOrganizationTypeList
{
    public class GetOrganizationTypeListQueryHandler
        : IRequestHandler<GetOrganizationTypeListQuery, GrpcPagedOrganizationTypeResponse>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public GetOrganizationTypeListQueryHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcPagedOrganizationTypeResponse> Handle(
            GetOrganizationTypeListQuery request,
            CancellationToken cancellationToken
        )
        {
            var search = request.Search?.ToLower();

            Expression<Func<OrganizationType, bool>> predicate = c =>
                (string.IsNullOrEmpty(search) ||
                 c.Name.ToLower().Contains(search));

            Expression<Func<OrganizationType, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    "id" => c => c.Id,
                    _ => c => c.Name,
                };


            var paged = await _unitOfWork.OrganizationTypes.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                },
                sortExpression: sortExpression,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new GrpcPagedOrganizationTypeResponse
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var item in paged.Items)
            {
                var organizationType = new GrpcOrganizationTypeModel
                {
                    Id = item.Id,
                    Name = item.Name,
                };

                response.Items.Add(organizationType);
            }

            return response;
        }
    }
}