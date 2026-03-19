using Identity.Application.Groups.Dtos;
using MediatR;

namespace Identity.Application.Groups.Commands.CreateGroup;

public record CreateGroupCommand : IRequest<GroupDto>
{
    public int OrganizationId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? Code { get; init; }
    public Guid CreatedByUserId { get; init; }
}

