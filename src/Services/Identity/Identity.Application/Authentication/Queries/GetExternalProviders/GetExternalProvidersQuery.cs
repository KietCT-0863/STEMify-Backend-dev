using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Authentication.Queries.GetExternalProviders;

/// <summary>
/// Query to get all external authentication providers linked to a user
/// </summary>
public record GetExternalProvidersQuery : IRequest<List<ExternalProviderDto>>
{
    /// <summary>
    /// The user ID to get external providers for
    /// </summary>
    public Guid UserId { get; init; }

    public GetExternalProvidersQuery(Guid userId)
    {
        UserId = userId;
    }
}
