using MediatR;
using Identity.Application.Dtos.BulkProvisioning;

namespace Identity.Application.Queries.BulkProvisioning.ListInvitations;

public class ListInvitationsQuery : IRequest<InvitationsPageDto>
{
    public int OrganizationId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}


