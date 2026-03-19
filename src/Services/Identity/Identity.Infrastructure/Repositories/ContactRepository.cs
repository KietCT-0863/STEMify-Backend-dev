using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Infrastructure.Abstractions.Persistence.EfCore;
using Sieve.Services;

namespace Identity.Infrastructure.Repositories
{
    public class ContactRepository
    : EfRepositoryBase<ApplicationDbContext, Contact, int>,
        IContactRepository
    {
        public ContactRepository(ApplicationDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
