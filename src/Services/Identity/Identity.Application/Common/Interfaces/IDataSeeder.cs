namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Application layer interface for data seeding operations
/// Following clean architecture - interfaces belong in Application layer
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Seeds the initial data required for the application
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Seeds development-specific sample data
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedSampleDataAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for individual seeding strategies
/// </summary>
public interface ISeedingStrategy
{
    /// <summary>
    /// The order in which this strategy should be executed
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Execute the seeding strategy
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for role seeding operations
/// </summary>
public interface IRoleSeeder : ISeedingStrategy
{
    /// <summary>
    /// Seeds system roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedRolesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for user seeding operations
/// </summary>
public interface IUserSeeder : ISeedingStrategy
{
    /// <summary>
    /// Seeds default system users
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedDefaultUsersAsync(CancellationToken cancellationToken = default);
    Task SeedDefaultUsersAsyncV2(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for OpenIddict/OAuth seeding operations
/// </summary>
public interface IOAuthSeeder : ISeedingStrategy
{
    /// <summary>
    /// Seeds OAuth applications and scopes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SeedOAuthConfigurationAsync(CancellationToken cancellationToken = default);
}
