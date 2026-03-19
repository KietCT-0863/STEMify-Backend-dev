using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Groups.Dtos;
using Identity.Application.ReadModels;
using Identity.Domain.Enums;
using MediatR;
using Shared.Enums;

namespace Identity.Application.Groups.Queries.GetGroupById;

public class GetGroupByIdQueryHandler : IRequestHandler<GetGroupByIdQuery, GroupDetailDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;

    public GetGroupByIdQueryHandler(
        IGroupRepository groupRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository)
    {
        _groupRepository = groupRepository;
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
    }

    public async Task<GroupDetailDto> Handle(
        GetGroupByIdQuery request,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.FindByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new NotFoundException($"Group with ID {request.GroupId} not found");
        
        
        var allStudentsInGroup = await _organizationUserRepository.GetStudentsByGroupIdAsync(
            request.GroupId,
            activeOnly: false,
            cancellationToken);
        
        var totalStudentCount = allStudentsInGroup
            .Count(ou => ou.OrganizationRole == OrganizationRole.Student);
        
        var students = await _organizationUserRepository.GetStudentsByGroupIdAsync(
            request.GroupId,
            activeOnly: request.ActiveOnly,
            cancellationToken);

        var studentIds = students.Select(s => s.Id).ToList();

        IReadOnlyList<OrganizationUserLicenseReadModel> licenseProjections;
        if (request.SubscriptionOrderId.HasValue)
        {
            licenseProjections = await _licenseReadRepository.GetBySubscriptionOrderIdAsync(
                request.SubscriptionOrderId.Value,
                cancellationToken);
            
            licenseProjections = licenseProjections
                .Where(lp => studentIds.Contains(lp.OrganizationUserId))
                .ToList();
        }
        else
        {
            licenseProjections = await _licenseReadRepository.GetByOrganizationIdAsync(
                group.OrganizationId,
                cancellationToken);
            
            licenseProjections = licenseProjections
                .Where(lp => studentIds.Contains(lp.OrganizationUserId))
                .ToList();
        }

        var licensesByOrgUserId = licenseProjections
            .GroupBy(lp => lp.OrganizationUserId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(lp => lp.AssignedAt).ToList());

        var studentDtos = students
            .Where(ou =>
            {
                if (ou.OrganizationRole != OrganizationRole.Student)
                    return false;
                
                if (request.SubscriptionOrderId.HasValue)
                {
                    if (!licensesByOrgUserId.TryGetValue(ou.Id, out var licenses) || !licenses.Any())
                        return false;
                    
                   var hasLicenseInSubscription = licenses.Any(lp => 
                        lp.SubscriptionOrderId == request.SubscriptionOrderId.Value);
                    
                    if (!hasLicenseInSubscription)
                        return false;
                }
                
                if (!request.ActiveOnly)
                    return true;
                
                
                if (!licensesByOrgUserId.TryGetValue(ou.Id, out var licensesForActiveCheck))
                    return false;
                
                var isActive = licensesForActiveCheck.Any(lp => lp.Status == LicenseAssignmentStatus.Active);
                return isActive == request.ActiveOnly;
            })
            .Select(ou =>
            {
                var licenses = licensesByOrgUserId.TryGetValue(ou.Id, out var list) ? list : [];
                
                IEnumerable<OrganizationUserLicenseReadModel> relevantLicenses = licenses;
                if (request.SubscriptionOrderId.HasValue)
                {
                    relevantLicenses = licenses
                        .Where(lp => lp.SubscriptionOrderId == request.SubscriptionOrderId.Value)
                        .OrderByDescending(lp => lp.Status == LicenseAssignmentStatus.Active)
                        .ThenByDescending(lp => lp.AssignedAt);
                }
                
                var activeLicense = relevantLicenses.FirstOrDefault(lp => lp.Status == LicenseAssignmentStatus.Active);
                var fallbackLicense = relevantLicenses.FirstOrDefault() ?? licenses.FirstOrDefault();
                var isActive = activeLicense != null;
                
                return new GroupStudentDto
                {
                    OrganizationUserId = ou.Id,
                    UserId = ou.UserId,
                    Email = ou.User.Email ?? string.Empty,
                    UserName = ou.User.UserName ?? string.Empty,
                    FullName = ou.User.FullName ?? string.Empty,
                    SubscriptionOrderId = activeLicense?.SubscriptionOrderId ?? fallbackLicense?.SubscriptionOrderId,
                    JoinedAt = ou.JoinedAt,
                    IsActive = isActive
                };
            })
            .OrderBy(s => s.FullName)
            .ThenBy(s => s.Email)
            .ToList();

        return new GroupDetailDto
        {
            Id = group.Id,
            OrganizationId = group.OrganizationId,
            Name = group.Name,
            Description = group.Description,
            Code = group.Code,
            Status = group.Status,
            CreatedByUserId = group.CreatedByUserId,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Students = studentDtos,
            StudentCount = studentDtos.Count,
            TotalStudentCount = totalStudentCount,
            FilteredSubscriptionOrderId = request.SubscriptionOrderId
        };
    }
}

