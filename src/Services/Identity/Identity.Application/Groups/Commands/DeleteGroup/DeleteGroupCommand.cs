using MediatR;

namespace Identity.Application.Groups.Commands.DeleteGroup;

public record DeleteGroupCommand : IRequest<bool>
{
    public int GroupId { get; init; }
}

