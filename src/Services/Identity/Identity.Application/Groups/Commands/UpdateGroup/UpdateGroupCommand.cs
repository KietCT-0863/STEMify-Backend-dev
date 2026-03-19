using Identity.Application.Groups.Dtos;
using MediatR;

namespace Identity.Application.Groups.Commands.UpdateGroup;

public record UpdateGroupCommand : IRequest<GroupDto>
{
    public int GroupId { get; set; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
}

