using Identity.Application.Groups.Dtos;
using MediatR;

namespace Identity.Application.Groups.Commands.CreateGroupWithStudents;

public record CreateGroupWithStudentsCommand : IRequest<GroupDto>
{
    public int OrganizationId { get; init; }
    public string Name { get; init; } = null!;
    public string Code { get; init; } = null!;
    public int? Grade { get; init; }
    public string? Description { get; init; }
    public Guid CreatedByUserId { get; init; }
    public List<Guid> StudentIds { get; init; } = new();
    public int? SubscriptionOrderId { get; init; }
    public string LicenseType { get; init; } = "Student";
}

