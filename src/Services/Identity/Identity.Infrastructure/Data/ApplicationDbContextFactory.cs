using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext
/// Used by EF Core tools for migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a default connection string for design-time operations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=STEMifyIdentity;Username=postgres;Password=12345;Include Error Detail=true;"
        );

        // Create a minimal service provider for design-time (migrations)
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder => builder.AddConsole());
        
        var serviceProvider = serviceCollection.BuildServiceProvider();

        return new ApplicationDbContext(optionsBuilder.Options, serviceProvider);
    }
}
