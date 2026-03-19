using Infrastructure.Common.Paging;
using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Features.Component.Queries;
using Shared.Protos.Product;
using System.Linq.Expressions;

namespace Product.Application.Features.Component.Handlers
{
    public class GetComponentListQueryHandler
        : IRequestHandler<GetComponentListQuery, PagedComponentList>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public GetComponentListQueryHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedComponentList> Handle(
            GetComponentListQuery request,
            CancellationToken cancellationToken
        )
        {
            Expression<Func<Domain.Entities.Component, bool>> predicate = c =>
                (string.IsNullOrEmpty(request.Search) || c.Name.ToLower().Contains(request.Search));

            var paged = await _unitOfWork.Components.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 50,
                },
                sortExpression: c => c.Id,
                predicate: predicate,
                cancellationToken: cancellationToken
            );


            var response = new PagedComponentList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var component in paged.Items)
            {
                var ComponentResponse = new ComponentResponse
                {
                    Id = component.Id,
                    ImageUrl = component.ImageUrl,
                    Description = component.Description,
                    Name = component.Name,
                };

                response.Items.Add(ComponentResponse);
            }

            return response;
        }
    }
}
