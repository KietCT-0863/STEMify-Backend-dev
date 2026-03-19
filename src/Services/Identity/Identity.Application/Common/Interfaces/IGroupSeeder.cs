namespace Identity.Application.Common.Interfaces;

public interface IGroupSeeder : ISeedingStrategy
{
    Task SeedGroupsAsync(CancellationToken cancellationToken = default);
}

