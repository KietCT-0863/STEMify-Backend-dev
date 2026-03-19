using Identity.Application.Dtos.BulkProvisioning;
using MediatR;

namespace Identity.Application.Queries.BulkProvisioning.ValidateInvitationToken;

public class ValidateInvitationTokenQuery : IRequest<InvitationValidationDto>
{
    public string Token { get; set; } = null!;
}
