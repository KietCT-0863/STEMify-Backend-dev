using System;
using System.Collections.Generic;
using System.Linq;
using Identity.Application.Common.Interfaces.Repositories;
using MediatR;

namespace Identity.Application.Users.Queries.GetOrganizationAdmins;

public record GetOrganizationAdminsQuery(int OrganizationId) : IRequest<IReadOnlyList<OrganizationAdminDto>>;

public record OrganizationAdminDto(string UserId, string Email, string FullName);

public class GetOrganizationAdminsQueryHandler
    : IRequestHandler<GetOrganizationAdminsQuery, IReadOnlyList<OrganizationAdminDto>>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;

    public GetOrganizationAdminsQueryHandler(IOrganizationUserRepository organizationUserRepository)
    {
        _organizationUserRepository = organizationUserRepository;
    }

    public async Task<IReadOnlyList<OrganizationAdminDto>> Handle(
        GetOrganizationAdminsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.OrganizationId <= 0)
        {
            return Array.Empty<OrganizationAdminDto>();
        }

        var admins = await _organizationUserRepository.GetOrganizationAdminsAsync(
            request.OrganizationId,
            cancellationToken);

        if (admins == null || admins.Count == 0)
        {
            return Array.Empty<OrganizationAdminDto>();
        }

        var results = admins
            .Where(admin => admin.User != null)
            .Select(admin =>
            {
                var fullName = !string.IsNullOrWhiteSpace(admin.User.FullName)
                    ? admin.User.FullName
                    : $"{admin.User.FirstName} {admin.User.LastName}".Trim();

                return new OrganizationAdminDto(
                    admin.UserId.ToString(),
                    admin.User.Email ?? string.Empty,
                    fullName);
            })
            .DistinctBy(admin => admin.UserId)
            .ToList();

        return results;
    }
}

