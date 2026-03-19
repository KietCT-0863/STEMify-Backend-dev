using MediatR;

namespace Identity.Application.Groups.Commands.AddStudentsToGroup;

public record AddStudentsToGroupCommand : IRequest<bool>
{
    public int GroupId { get; init; }
    public List<Guid> StudentIds { get; init; } = new();
}

