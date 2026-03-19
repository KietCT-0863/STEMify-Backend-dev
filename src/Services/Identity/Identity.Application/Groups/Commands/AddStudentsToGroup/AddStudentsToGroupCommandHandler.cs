using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Groups.Commands.AddStudentsToGroup;

public class AddStudentsToGroupCommandHandler : IRequestHandler<AddStudentsToGroupCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<AddStudentsToGroupCommandHandler> _logger;

    public AddStudentsToGroupCommandHandler(
        IGroupRepository groupRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<AddStudentsToGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        AddStudentsToGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.FindByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new NotFoundException($"Group with ID {request.GroupId} not found");

        if (group.Status == Domain.Enums.GroupStatus.Archived)
            throw new InvalidOperationException("Cannot add students to archived group");

        var orgUsers = await _organizationUserRepository.GetByIdsAsync(request.StudentIds, cancellationToken);
        if (orgUsers.Count != request.StudentIds.Count)
        {
            var foundIds = orgUsers.Select(ou => ou.Id).ToList();
            var missingIds = request.StudentIds.Except(foundIds).ToList();
            throw new NotFoundException($"Some student IDs are invalid: {string.Join(", ", missingIds)}");
        }

        var activeOrgUserIds = await _licenseReadRepository.GetActiveOrganizationUserIdsAsync(
            organizationId: group.OrganizationId,
            subscriptionOrderId: null,
            cancellationToken);

        var invalidUsers = orgUsers
            .Where(ou =>
            {
                var isActive = activeOrgUserIds.Contains(ou.Id);
                return ou.OrganizationId != group.OrganizationId
                       || ou.OrganizationRole != OrganizationRole.Student
                      // || !isActive
                       ;
            })
            .ToList();

        if (invalidUsers.Any())
        {
            var invalidIds = invalidUsers.Select(ou => ou.Id).ToList();
            throw new InvalidOperationException(
                $"Some users are not valid active students in this organization: {string.Join(", ", invalidIds)}");
        }

        // // Check if any students already in another group
        // var studentsInOtherGroups = orgUsers
        //     .Where(ou => ou.GroupId.HasValue && ou.GroupId != request.GroupId)
        //     .ToList();

        // if (studentsInOtherGroups.Any())
        // {
        //     var conflictingIds = studentsInOtherGroups.Select(ou => ou.Id).ToList();
        //     throw new InvalidOperationException(
        //         $"Some students are already in another group: {string.Join(", ", conflictingIds)}");
        // }

        // Filter students that are not already in this group
        var studentsToAdd = orgUsers
            .Where(ou => ou.GroupId != request.GroupId)
            .ToList();

        if (!studentsToAdd.Any())
        {
            _logger.LogInformation("All students are already in group {GroupId}", request.GroupId);
            return true; // Idempotent operation
        }

        // Assign students to group
        foreach (var student in studentsToAdd)
        {
            student.AssignToGroup(request.GroupId);
        }

        await _organizationUserRepository.UpdateRangeAsync(studentsToAdd, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added {Count} students to group {GroupId}",
            studentsToAdd.Count,
            request.GroupId);

        return true;
    }
}

