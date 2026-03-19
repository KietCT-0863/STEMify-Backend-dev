using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Groups.Commands.DeleteGroup;

public class DeleteGroupCommandHandler : IRequestHandler<DeleteGroupCommand, bool>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public DeleteGroupCommandHandler(
        IGroupRepository groupRepository,
        IOrganizationUserRepository organizationUserRepository,
        IIdentityUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _organizationUserRepository = organizationUserRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(
        DeleteGroupCommand request,
        CancellationToken cancellationToken)
    {
        var group = await _groupRepository.FindByIdAsync(request.GroupId, cancellationToken);
        if (group == null)
            throw new NotFoundException($"Group with ID {request.GroupId} not found");

        // (soft delete)
        group.Archive();

        var students = await _organizationUserRepository.GetStudentsByGroupIdAsync(
            request.GroupId,
            activeOnly: false, 
            cancellationToken);

        foreach (var student in students)
        {
            student.AssignToGroup((int?)null);
        }

        await _groupRepository.UpdateAsync(group, cancellationToken);

        if (students.Any())
        {
            await _organizationUserRepository.UpdateRangeAsync(students, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

