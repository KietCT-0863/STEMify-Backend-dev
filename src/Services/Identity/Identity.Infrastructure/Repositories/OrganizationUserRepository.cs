using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Infrastructure.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;

namespace Identity.Infrastructure.Repositories;

public class OrganizationUserRepository : EfRepositoryBase<ApplicationDbContext, OrganizationUser, Guid>, IOrganizationUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;

    public OrganizationUserRepository(
        ApplicationDbContext dbContext, 
        ISieveProcessor sieveProcessor,
        IOrganizationUserLicenseReadRepository licenseReadRepository)
        : base(dbContext, sieveProcessor)
    {
        _dbContext = dbContext;
        _licenseReadRepository = licenseReadRepository;
    }

    public async Task<OrganizationUser?> GetByUserAndOrganizationAsync(
        Guid userId,
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(ou => ou.User)
            .FirstOrDefaultAsync(ou => ou.UserId == userId
                && ou.OrganizationId == organizationId,
                cancellationToken);
    }

   
    public async Task<bool> IsUserInOrganizationAsync(
        Guid userId,
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var orgUser = await GetByUserAndOrganizationAsync(userId, organizationId, cancellationToken);
        if (orgUser == null)
            return false;

        return await _licenseReadRepository.IsOrganizationUserActiveAsync(
            orgUser.Id,
            cancellationToken);
    }

   
    public async Task<List<OrganizationUser>> GetByUserIdAsync(
        Guid userId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(ou => ou.User)
            .Where(ou => ou.UserId == userId);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: null,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        return await query
            .OrderByDescending(ou => ou.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    
    public async Task<List<OrganizationUser>> GetByOrganizationAsync(
        int organizationId,
        bool activeOnly = true,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(ou => ou.User)
            .Where(ou => ou.OrganizationId == organizationId);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections for this organization
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: organizationId,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        return await query
            .OrderByDescending(ou => ou.JoinedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

   
    public async Task<bool> IsUserOrganizationAdminAsync(
        Guid userId,
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        var orgUser = await GetByUserAndOrganizationAsync(userId, organizationId, cancellationToken);
        if (orgUser == null || orgUser.OrganizationRole != OrganizationRole.OrganizationAdmin)
            return false;

        // Check if OrganizationUser has any active license
        return await _licenseReadRepository.IsOrganizationUserActiveAsync(
            orgUser.Id,
            cancellationToken);
    }

   
    public async Task<int> CountByOrganizationAsync(
        int organizationId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(ou => ou.OrganizationId == organizationId);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections for this organization
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: organizationId,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<OrganizationUser>> GetByOrganizationAndRoleAsync(
        int organizationId,
        OrganizationRole role,
        CancellationToken cancellationToken = default)
    {
        // Get active OrganizationUserIds from license projections for this organization
        var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
            organizationId: organizationId,
            subscriptionOrderId: null,
            cancellationToken);

        return await DbSet
            .Include(ou => ou.User)
            .Where(ou => ou.OrganizationId == organizationId
                && ou.OrganizationRole == role
                && activeOrgUserIds.Contains(ou.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OrganizationUser>> GetOrganizationAdminsAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        // Get active OrganizationUserIds from license projections for this organization
        var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
            organizationId: organizationId,
            subscriptionOrderId: null,
            cancellationToken);

        return await DbSet
            .Include(ou => ou.User)
            .Where(ou => ou.OrganizationId == organizationId
                && ou.OrganizationRole == OrganizationRole.OrganizationAdmin
                && activeOrgUserIds.Contains(ou.Id))
            .ToListAsync(cancellationToken);
    }

    
    public async Task<int> CountActiveUsersAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        // Get active OrganizationUserIds from license projections for this organization
        var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
            organizationId: organizationId,
            subscriptionOrderId: null,
            cancellationToken);

        return await DbSet
            .CountAsync(ou => ou.OrganizationId == organizationId 
                           && activeOrgUserIds.Contains(ou.Id),
                cancellationToken);
    }

   
    public async Task<(List<OrganizationUser> Items, int TotalCount)> SearchAsync(
        int? subscriptionOrderId,
        string? licenseType,
        string? search,
        string? orderBy,
        string? status,
        string? role,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Get active OrganizationUserIds from license projections
        var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
            organizationId: null,
            subscriptionOrderId: subscriptionOrderId,
            cancellationToken);

        var query = DbSet
            .Include(ou => ou.User)
            .Where(ou => activeOrgUserIds.Contains(ou.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(ou =>
                (ou.User.Email != null && ou.User.Email.ToLower().Contains(term)) ||
                (ou.User.UserName != null && ou.User.UserName.ToLower().Contains(term)) ||
                (ou.User.FullName != null && ou.User.FullName.ToLower().Contains(term)) ||
                (ou.User.FirstName != null && ou.User.FirstName.ToLower().Contains(term)) ||
                (ou.User.LastName != null && ou.User.LastName.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            if (Enum.TryParse<UserStatus>(s, true, out var parsedStatus))
            {
                query = query.Where(ou => ou.User.Status == parsedStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim();
            if (Enum.TryParse<UserRole>(r, true, out var parsedRole))
            {
                query = query.Where(ou => ou.User.Role == parsedRole);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        IOrderedQueryable<OrganizationUser> ordered;
        switch ((orderBy ?? string.Empty).Trim().ToLower())
        {
            case "fullname":
                ordered = query.OrderBy(ou => ou.User.FullName);
                break;
            case "username":
                ordered = query.OrderBy(ou => ou.User.UserName);
                break;
            case "createdat":
                ordered = query.OrderBy(ou => ou.User.CreatedAt);
                break;
            case "lastloginat":
                ordered = query.OrderByDescending(ou => ou.User.LastLoginAt);
                break;
            case "email":
            default:
                ordered = query.OrderBy(ou => ou.User.Email);
                break;
        }

        var items = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(List<Guid> UserIds, int TotalCount)> GetDistinctUserIdsByOrganizationAsync(
        int organizationId,
        bool activeOnly = true,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(ou => ou.OrganizationId == organizationId);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections for this organization
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: organizationId,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        var totalCount = await query
            .Select(ou => ou.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var userIds = await query
            .GroupBy(ou => ou.UserId)
            .OrderByDescending(g => g.Max(ou => ou.JoinedAt)) 
            .Select(g => g.Key)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (userIds, totalCount);
    }

    public async Task<List<OrganizationUser>> GetSubscriptionsForUsersAsync(
        int organizationId,
        List<Guid> userIds,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        if (userIds == null || userIds.Count == 0)
            return new List<OrganizationUser>();

        var query = DbSet
            .Include(ou => ou.User)
            .Include(ou => ou.Group)
            .Where(ou => ou.OrganizationId == organizationId && userIds.Contains(ou.UserId));

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections for this organization
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: organizationId,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        return await query
            .OrderByDescending(ou => ou.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OrganizationUser>> GetStudentsByGroupIdAsync(
        int groupId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(ou => ou.User)
            .Where(ou => ou.GroupId == groupId 
                      && ou.OrganizationRole == OrganizationRole.Student);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: null,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        return await query
            .OrderBy(ou => ou.User.Email)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountStudentsByGroupIdAsync(
        int groupId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Where(ou => ou.GroupId == groupId 
                      && ou.OrganizationRole == OrganizationRole.Student);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: null,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> CountStudentsByGroupIdsAsync(
        List<int> groupIds,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        if (groupIds == null || groupIds.Count == 0)
            return new Dictionary<int, int>();

        var query = DbSet
            .Where(ou => ou.GroupId.HasValue
                      && groupIds.Contains(ou.GroupId.Value)
                      && ou.OrganizationRole == OrganizationRole.Student);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: null,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        var counts = await query
            .GroupBy(ou => ou.GroupId!.Value)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(c => c.GroupId, c => c.Count);
    }

    public async Task<Dictionary<int, List<OrganizationUser>>> GetStudentsByGroupIdsAsync(
        List<int> groupIds,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        if (groupIds == null || groupIds.Count == 0)
            return new Dictionary<int, List<OrganizationUser>>();

        var query = DbSet
            .Include(ou => ou.User)
            .Where(ou => ou.GroupId.HasValue
                      && groupIds.Contains(ou.GroupId.Value)
                      && ou.OrganizationRole == OrganizationRole.Student);

        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections
            var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: null,
                subscriptionOrderId: null,
                cancellationToken);
            
            query = query.Where(ou => activeOrgUserIds.Contains(ou.Id));
        }

        var students = await query.ToListAsync(cancellationToken);

        return students
            .GroupBy(ou => ou.GroupId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(ou => ou.User.Email)
                    .ToList());
    }

    public async Task<List<OrganizationUser>> GetByIdsAsync(
        List<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
            return new List<OrganizationUser>();

        return await DbSet
            .Include(ou => ou.User)
            .Where(ou => ids.Contains(ou.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<OrganizationUser>> GetBySubscriptionOrderIdAsync(
        int subscriptionOrderId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        HashSet<Guid> orgUserIds;
        
        if (activeOnly)
        {
            // Get active OrganizationUserIds from license projections for this subscription
            orgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
                organizationId: null,
                subscriptionOrderId: subscriptionOrderId,
                cancellationToken);
        }
        else
        {
            // If not activeOnly, get all OrganizationUsers that have any license in this subscription
            var allOrgUserIds = await _dbContext.OrganizationUserLicenseReadModels
                .AsNoTracking()
                .Where(p => p.SubscriptionOrderId == subscriptionOrderId)
                .Select(p => p.OrganizationUserId)
                .Distinct()
                .ToListAsync(cancellationToken);
            
            orgUserIds = allOrgUserIds.ToHashSet();
        }

        if (orgUserIds.Count == 0)
            return new List<OrganizationUser>();

        var query = DbSet
            .Include(ou => ou.User)
            .Where(ou => orgUserIds.Contains(ou.Id));

        return await query
            .OrderByDescending(ou => ou.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateRangeAsync(
        List<OrganizationUser> organizationUsers,
        CancellationToken cancellationToken = default)
    {
        if (organizationUsers == null || organizationUsers.Count == 0)
            return Task.CompletedTask;

        DbSet.UpdateRange(organizationUsers);
        return Task.CompletedTask;
    }
}
