using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.Groups.Dtos;
using Identity.Application.Groups.Queries.GetGroupById;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Groups.Queries.GetGroupList;

public class GetGroupListQueryHandler : IRequestHandler<GetGroupListQuery, PagedResult<GroupListItemDto>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;

    public GetGroupListQueryHandler(
        IGroupRepository groupRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository)
    {
        _groupRepository = groupRepository;
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
    }

    public async Task<PagedResult<GroupListItemDto>> Handle(
        GetGroupListQuery request,
        CancellationToken cancellationToken)
    {
        if (request.OrganizationId <= 0)
        {
            return new PagedResult<GroupListItemDto>(
                Enumerable.Empty<GroupListItemDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        GroupGrade? gradeFilter = null;
        if (request.Grade.HasValue && Enum.IsDefined(typeof(GroupGrade), request.Grade.Value))
        {
            gradeFilter = (GroupGrade)request.Grade.Value;
        }

        var groups = await _groupRepository.GetByOrganizationAsync(
            request.OrganizationId,
            request.IncludeArchived,
            gradeFilter,
            cancellationToken);

        if (!groups.Any())
        {
            return new PagedResult<GroupListItemDto>(
                Enumerable.Empty<GroupListItemDto>(),
                0,
                request.PageNumber,
                request.PageSize);
        }

        var totalCount = groups.Count;

        var pagedGroups = groups
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var pagedGroupIds = pagedGroups
            .Select(g => g.Id)
            .ToList();

        var studentCounts = await _organizationUserRepository.CountStudentsByGroupIdsAsync(
            pagedGroupIds,
            activeOnly: false,
            cancellationToken);

        var studentsByGroup = await _organizationUserRepository.GetStudentsByGroupIdsAsync(
            pagedGroupIds,
            activeOnly: false,
            cancellationToken);

        var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
            organizationId: null,
            subscriptionOrderId: null,
            cancellationToken);

        var pagedItems = pagedGroups
            .Select(g =>
            {
                var students = studentsByGroup.TryGetValue(g.Id, out var orgUsers)
                    ? orgUsers
                        .Select(ou =>
                        {
                            // IsActive is determined by license projection
                            var isActive = activeOrgUserIds.Contains(ou.Id);
                            
                            return new GroupStudentDto
                            {
                                OrganizationUserId = ou.Id,
                                UserId = ou.UserId,
                                Email = ou.User.Email ?? string.Empty,
                                UserName = ou.User.UserName ?? string.Empty,
                                FullName = ou.User.FullName ?? string.Empty,
                                SubscriptionOrderId = null,
                                JoinedAt = ou.JoinedAt,
                                IsActive = isActive
                            };
                        })
                        .OrderBy(s => s.FullName)
                        .ThenBy(s => s.Email)
                        .ToList()
                    : new List<GroupStudentDto>();

                return new GroupListItemDto
                {
                    Id = g.Id,
                    OrganizationId = g.OrganizationId,
                    Name = g.Name,
                    Description = g.Description,
                    Code = g.Code,
                    Status = g.Status.ToString(),
                    StudentCount = studentCounts.GetValueOrDefault(g.Id, 0),
                    CreatedAt = g.CreatedAt,
                    UpdatedAt = g.UpdatedAt,
                    Students = students
                };
            })
            .ToList();

        return new PagedResult<GroupListItemDto>(
            pagedItems,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}

