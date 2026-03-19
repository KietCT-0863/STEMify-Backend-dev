using Contracts.Abstractions.Persistence;
using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces.Repositories
{

    public interface IContactRepository : IRepositoryBaseAsync<Contact, int> { }

}
