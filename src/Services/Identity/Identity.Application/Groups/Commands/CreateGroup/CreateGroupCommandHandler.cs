using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Groups.Dtos;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Groups.Commands.CreateGroup;

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, GroupDto>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public CreateGroupCommandHandler(
        IGroupRepository groupRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupDto> Handle(
        CreateGroupCommand request,
        CancellationToken cancellationToken)
    {
        // Validate name uniqueness
        var isNameUnique = await _groupRepository.IsNameUniqueInOrganizationAsync(
            request.OrganizationId,
            request.Name,
            cancellationToken: cancellationToken);

        if (!isNameUnique)
            throw new InvalidOperationException($"Group name '{request.Name}' already exists in this organization");

        // Validate code uniqueness (if provided)
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var isCodeUnique = await _groupRepository.IsCodeUniqueInOrganizationAsync(
                request.OrganizationId,
                request.Code,
                cancellationToken: cancellationToken);

            if (!isCodeUnique)
                throw new InvalidOperationException($"Mã nhóm '{request.Code}' đã tồn tại trong tổ chức");
        }

        var group = Group.CreateWithCode(
            request.OrganizationId,
            request.Name,
            request.CreatedByUserId,
            request.Description,
            request.Code);

        await _groupRepository.AddAsync(group, cancellationToken);
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

