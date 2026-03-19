using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product.Application.Common.Interfaces;
using Product.Domain.Entities;
using Product.Domain.Enums;
using Shared.Protos.Product;
using System.Linq.Expressions;

namespace Product.Application.Features.KitProducts.Queries.GetKitProductList
{
    public class QueryKitProductsQueryHandler
    : IRequestHandler<GetKitProductListQuery, PagedKitList>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public QueryKitProductsQueryHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedKitList> Handle(
            GetKitProductListQuery request,
            CancellationToken cancellationToken
        )
        {
            Expression<Func<KitProduct, bool>> predicate = c =>
                (string.IsNullOrEmpty(request.Search) || c.Name.ToLower().Contains(request.Search)) &&
                (!request.AgeRangeId.HasValue || c.AgeRangeId == request.AgeRangeId.Value) &&
                (request.Status.HasValue ? c.Status == request.Status.Value : c.Status != KitProductStatus.Archived);

            Expression<Func<KitProduct, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "name" => c => c.Name,
                    _ => c => c.CreatedDate,
                };

            // Define projection function
            Func<IQueryable<KitProduct>, IQueryable<KitResponseV2>> projectionFunc = query =>
                query
                .Include(c => c.KitImages)
                .Select(kit => new KitResponseV2
                {
                    Id = kit.Id,
                    Name = kit.Name,
                    Description = kit.Description,
                    ImageUrl = kit.KitImages.OrderBy(x => x.Id).Select(x => x.ImageUrl).FirstOrDefault(),
                    AltText = kit.KitImages.OrderBy(x => x.Id).Select(x => x.AltText).FirstOrDefault(),
                    Status = kit.Status.ToString(),
                    CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                                kit.CreatedDate
                            ),
                    LastModifiedDate =
                                kit.LastModifiedDate != null
                                    ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                                        kit.LastModifiedDate.Value
                                    )
                                    : null,
                });

            var paged = await _unitOfWork.KitProducts.GetByPageFilter(
                pageRequest: new PageRequest
                {
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                },
                projectionFunc: projectionFunc,
                sortExpression: sortExpression,
                predicate: predicate,
                descending: request.IsDescending,
                cancellationToken: cancellationToken
            );

            var response = new PagedKitList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };
            response.Items.AddRange(paged.Items);

            return response;
        }
    }
}
