using Contracts.Abstractions.Persistence;

namespace Order.Application.Common.Interfaces.Repositories
{
    public interface ILicenseAssignmentRepository : IRepositoryBaseAsync<Domain.Entities.LicenseAssignment, int> { }
}
