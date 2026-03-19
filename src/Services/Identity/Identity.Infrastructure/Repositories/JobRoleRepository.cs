using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Infrastructure.Abstractions.Persistence.EfCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Sieve.Services;

namespace Identity.Infrastructure.Repositories
{
    public class JobRoleRepository
    : EfRepositoryBase<ApplicationDbContext, JobRole, int>,
        IJobRoleRepository
    {
        public JobRoleRepository(ApplicationDbContext context, ISieveProcessor sieveProcessor)
            : base(context, sieveProcessor) { }
    }
}
