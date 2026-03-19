using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Groups.Dtos;
using MediatR;

namespace Identity.Application.Groups.Commands.UpdateGroup;

public class UpdateGroupCommandHandler : IRequestHandler<UpdateGroupCommand, GroupDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public UpdateGroupCommandHandler(
        IGroupRepository groupRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupDto> Handle(
        UpdateGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.FindByIdAsync(request.GroupId, cancellationToken);

        var isNameUnique = await _groupRepository.IsNameUniqueInOrganizationAsync(
            group.OrganizationId,
            request.Name,
            request.GroupId,
            cancellationToken);

        if (!isNameUnique)
            throw new InvalidOperationException($"Group name '{request.Name}' already exists in this organization");

        group.UpdateInfo(request.Name, request.Description);

        await _groupRepository.UpdateAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GroupDto
        {
            Id = group.Id,
            OrganizationId = group.OrganizationId,
            Name = group.Name,
            Description = group.Description,
            Code = group.Code,
            Status = group.Status,
            CreatedByUserId = group.CreatedByUserId,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };
    }
}

