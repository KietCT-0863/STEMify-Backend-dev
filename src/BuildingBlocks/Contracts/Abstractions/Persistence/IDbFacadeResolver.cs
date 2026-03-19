using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Contracts.Abstractions.Persistence
{
    public interface IDbFacadeResolver
    {
        DatabaseFacade Database { get; }
    }
}
