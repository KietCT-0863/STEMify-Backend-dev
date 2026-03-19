using Contracts.Abstractions.Persistence.EfCore;
using Product.Application.Common.Interfaces.Repositories;

namespace Product.Application.Common.Interfaces
{
    public interface IProductUnitOfWork : IEfUnitOfWork
    {
        IPlanRepository Plans { get; }
        IPlanBillingCycleRepository PlanBillingCycles { get; }
        IPlanCurriculumRepository PlanCurriculums { get; }
        IKitProductRepository KitProducts { get; }
        IKitImageRepository KitImages { get; }
        IComponentRepository Components { get; }
        IKitComponentRepository KitComponents { get; }
    }
}
