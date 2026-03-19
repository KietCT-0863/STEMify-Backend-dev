using Identity.Application.Common.Interfaces.Repositories;
using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Queries.GetOrganizationUserById;

public sealed class GetOrganizationUserQueryHandler
    : IRequestHandler<GetOrganizationUserQuery, OrganizationUserModel>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public GetOrganizationUserQueryHandler(
        IOrganizationUserRepository organizationUserRepository)
    {
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<OrganizationUserModel> Handle(
        GetOrganizationUserQuery request,
        CancellationToken cancellationToken)
    {
        var existingOrgUser = await _organizationUserRepository.GetByUserAndOrganizationAsync(
            request.UserId,
            request.OrganizationId,
            cancellationToken);

        if (existingOrgUser is null)
            throw new KeyNotFoundException(
                $"Organization user with UserId '{request.UserId}' " +
                $"and OrganizationId '{request.OrganizationId}' was not found.");


        return new OrganizationUserModel
        {
            UserId = existingOrgUser.UserId.ToString(),
            OrganizationUserId = existingOrgUser.Id.ToString(),
            OrganizationId = existingOrgUser.OrganizationId
        };
    }
}


