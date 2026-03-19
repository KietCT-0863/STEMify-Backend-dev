using MediatR;

namespace Identity.Application.Groups.Commands.RemoveStudentsFromGroup;

public record RemoveStudentsFromGroupCommand : IRequest<bool>
{
    public int GroupId { get; init; }
    public List<Guid> StudentIds { get; init; } = new(); 
}

