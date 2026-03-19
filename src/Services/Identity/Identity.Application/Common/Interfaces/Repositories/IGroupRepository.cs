using Contracts.Abstractions.Persistence;
using Identity.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Application.Common.Interfaces.Repositories;

public interface IGroupRepository : IRepositoryBaseAsync<Group, int>
{
    Task<List<Group>> GetByOrganizationAsync(
        int organizationId,
        bool includeArchived = false,
        GroupGrade? grade = default,
        CancellationToken cancellationToken = default);

    Task<Group?> GetByIdWithStudentsAsync(
        int groupId,
        CancellationToken cancellationToken = default);

    Task<bool> IsNameUniqueInOrganizationAsync(
        int organizationId,
        string name,
        int? excludeGroupId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsCodeUniqueInOrganizationAsync(
        int organizationId,
        string code,
        int? excludeGroupId = null,
        CancellationToken cancellationToken = default);

    Task<Group?> GetByOrganizationAndCodeAsync(
        int organizationId,
        string code,
        CancellationToken cancellationToken = default);
}

