using Contracts.Abstractions.Persistence.EfCore;
using Identity.Application.Common.Interfaces.Repositories;

namespace Identity.Application.Common.Interfaces;

public interface IIdentityUnitOfWork : IEfUnitOfWork
{
    IUserRepository Users { get; }
    IContactRepository Contacts { get; }
    IJobRoleRepository JobRoles { get; }
}
