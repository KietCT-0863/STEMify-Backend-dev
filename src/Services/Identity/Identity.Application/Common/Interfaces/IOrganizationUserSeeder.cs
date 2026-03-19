namespace Identity.Application.Common.Interfaces;

public interface IOrganizationUserSeeder : ISeedingStrategy
{
    Task SeedOrganizationUsersAsync(CancellationToken cancellationToken = default);
}

