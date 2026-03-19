using Identity.Application.FunctionalTests.Infrastructure;
using Identity.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Identity.Application.FunctionalTests.Common;

/// <summary>
/// Base class for integration tests
/// Provides common setup and database management
/// </summary>
public abstract class BaseIntegrationTest
    : IClassFixture<IdentityApplicationFactory>,
        IAsyncLifetime
{
    protected readonly IdentityApplicationFactory Factory;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;

    protected BaseIntegrationTest(IdentityApplicationFactory factory)
    {
        Factory = factory;
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    /// <typeparam name="T">Service type</typeparam>
    /// <returns>Service instance</returns>
    protected T GetRequiredService<T>()
        where T : notnull
    {
        return Scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Initialize test - reset database for test isolation
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    /// <summary>
    /// Cleanup test resources
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        Scope.Dispose();
        await Task.CompletedTask;
    }
}
