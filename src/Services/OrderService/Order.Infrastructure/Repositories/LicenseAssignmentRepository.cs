using Infrastructure.Abstractions.Persistence.EfCore;
using Order.Application.Common.Interfaces.Repositories;
using Order.Infrastructure.Persistence;
using Sieve.Services;

namespace Order.Infrastructure.Repositories
{
    public class LicenseAssignmentRepository
        : EfRepositoryBase<OrderDbContext, Domain.Entities.LicenseAssignment, int>,
        ILicenseAssignmentRepository
    {
        public LicenseAssignmentRepository(OrderDbContext context, ISieveProcessor sieveProcessor)
        : base(context, sieveProcessor) { }
    }
}
