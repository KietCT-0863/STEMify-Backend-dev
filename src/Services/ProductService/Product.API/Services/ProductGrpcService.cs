using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Product.Infrastructure.Persistence;
using Shared.Protos.Product;

namespace Product.API.Services
{
    public class ProductGrpcService : ProductService.ProductServiceBase
    {
        private readonly ProductDbContext _dbContext;

        public ProductGrpcService(ProductDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
        {
            // In the refactored architecture, Products are split into KitProducts and Plans.
            // When CartService queries a unified Product ID, we must attempt to resolve it from the domain entities.
            
            // 1. Try resolving as a KitProduct (commonly added from the STEM Kit shop)
            var kit = await _dbContext.KitProducts
                .Include(k => k.KitImages)
                .FirstOrDefaultAsync(k => k.Id == request.Id);

            if (kit != null)
            {
                return new ProductResponse
                {
                    Id = kit.Id,
                    Name = kit.Name ?? "Unnamed Kit",
                    Description = kit.Description ?? "",
                    ImageUrl = kit.KitImages.FirstOrDefault()?.ImageUrl ?? "",
                    Price = 0, // Domain entity KitProduct does not have a price attribute yet
                    Sku = $"KIT-{kit.Id}"
                };
            }

            // 2. Try resolving as a Plan Billing Cycle
            var planCycle = await _dbContext.PlanBillingCycles
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.Id == request.Id);

            if (planCycle != null && planCycle.Plan != null)
            {
                return new ProductResponse
                {
                    Id = planCycle.Id,
                    Name = planCycle.Plan.Name ?? "Unnamed Plan",
                    Description = planCycle.Plan.Description ?? "",
                    ImageUrl = "",
                    Price = (double)planCycle.Price,
                    Sku = $"PLAN-{planCycle.Id}"
                };
            }

            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID {request.Id} not found"));
        }
    }
}
