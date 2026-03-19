using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Dtos.BulkProvisioning;
using MediatR;

namespace Identity.Application.Queries.BulkProvisioning.ListInvitations;

public class ListInvitationsQueryHandler : IRequestHandler<ListInvitationsQuery, InvitationsPageDto>
{
    private readonly IInvitationRepository _invitationRepository;

    public ListInvitationsQueryHandler(IInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    public async Task<InvitationsPageDto> Handle(ListInvitationsQuery request, CancellationToken cancellationToken)
    {
        // For now list pending invitations; extend repository later for full-by-org listing with paging
        var pending = await _invitationRepository.GetPendingInvitationsAsync(request.OrganizationId, cancellationToken);
        var totalPending = await _invitationRepository.CountPendingAsync(request.OrganizationId, cancellationToken);

        var items = pending
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvitationSummaryDto
            {
                InvitationId = i.Id,
                InviteeEmail = i.InviteeEmail.Value,
                Accepted = i.AcceptedAt.HasValue,
                InvitedAt = i.CreatedAt
            })
            .ToList();

        return new InvitationsPageDto
        {
            Items = items,
            TotalCount = totalPending,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}


