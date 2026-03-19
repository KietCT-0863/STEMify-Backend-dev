using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Infrastructure.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;

namespace Identity.Infrastructure.Repositories;

public class GroupRepository : EfRepositoryBase<ApplicationDbContext, Group, int>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext dbContext, ISieveProcessor sieveProcessor)
        : base(dbContext, sieveProcessor)
    {
    }

    public async Task<List<Group>> GetByOrganizationAsync(
        int organizationId,
        bool includeArchived = false,
        GroupGrade? grade = default,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(g => g.OrganizationId == organizationId);

        if (!includeArchived)
            query = query.Where(g => g.Status == GroupStatus.Active);

        if (grade.HasValue)
        {
            query = query.Where(g => g.Grade == grade);
        }

        return await query
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Group?> GetByIdWithStudentsAsync(
        int groupId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(g => g.Students)
                .ThenInclude(ou => ou.User)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }

    public async Task<bool> IsNameUniqueInOrganizationAsync(
        int organizationId,
        string name,
        int? excludeGroupId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(g => g.OrganizationId == organizationId 
                     && g.Name.ToLower() == name.Trim().ToLower());

        if (excludeGroupId.HasValue)
            query = query.Where(g => g.Id != excludeGroupId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsCodeUniqueInOrganizationAsync(
        int organizationId,
        string code,
        int? excludeGroupId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return true; 

        var query = DbSet
            .Where(g => g.OrganizationId == organizationId 
                     && g.Code != null
                     && g.Code.ToLower() == code.Trim().ToLower());

        if (excludeGroupId.HasValue)
            query = query.Where(g => g.Id != excludeGroupId.Value);

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<Group?> GetByOrganizationAndCodeAsync(
        int organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                g => g.OrganizationId == organizationId && g.Code == code,
                cancellationToken);
    }
}

