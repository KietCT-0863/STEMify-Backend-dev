using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Groups.Commands.RemoveStudentsFromGroup;

public class RemoveStudentsFromGroupCommandHandler : IRequestHandler<RemoveStudentsFromGroupCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveStudentsFromGroupCommandHandler> _logger;

    public RemoveStudentsFromGroupCommandHandler(
        IGroupRepository groupRepository,
        IOrganizationUserRepository organizationUserRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<RemoveStudentsFromGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _organizationUserRepository = organizationUserRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(
        RemoveStudentsFromGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.FindByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new NotFoundException($"Group with ID {request.GroupId} not found");

        var orgUsers = await _organizationUserRepository.GetByIdsAsync(request.StudentIds, cancellationToken);
        if (orgUsers.Count != request.StudentIds.Count)
        {
            var foundIds = orgUsers.Select(ou => ou.Id).ToList();
            var missingIds = request.StudentIds.Except(foundIds).ToList();
            throw new NotFoundException($"Some student IDs are invalid: {string.Join(", ", missingIds)}");
        }

        var notInGroup = orgUsers
            .Where(ou => ou.GroupId != request.GroupId)
            .ToList();

        if (notInGroup.Any())
        {
            var invalidIds = notInGroup.Select(ou => ou.Id).ToList();
            throw new InvalidOperationException(
                $"Some students are not in this group: {string.Join(", ", invalidIds)}");
        }

        foreach (var student in orgUsers)
        {
            student.AssignToGroup((int?)null);
        }

        await _organizationUserRepository.UpdateRangeAsync(orgUsers, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Removed {Count} students from group {GroupId}",
            orgUsers.Count,
            request.GroupId);

        return true;
    }
}

