using Contracts.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;

namespace Order.Application.Common.Interfaces
{
    public interface IOrderUnitOfWork : IEfUnitOfWork
    {
        IOrderRepository Orders { get; }
        IContractRepository Contracts { get; }
        IOrganizationRepository Organizations { get; }
        ILicenseAssignmentRepository LicenseAssignments { get; }
        IOrganizationSubscriptionOrderRepository OrganizationSubscriptionOrders { get; }
        IOrganizationTypeRepository OrganizationTypes { get; }
        ISubscriptionOrderCurriculumRepository SubscriptionOrderCurriculums { get; }
    }
}
