using Identity.Application.Common.Interfaces;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Identity.Web.Services;

/// <summary>
/// Background service responsible for database initialization and seeding
/// </summary>
public class DatabaseInitializationHostedService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitializationHostedService> logger,
    DatabaseInitializationService dbInitService,
    IHostEnvironment environment
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting background database initialization...");

            await InitializeDatabaseAsync(stoppingToken);

            // Mark database as initialized for health checks
            dbInitService.MarkAsInitialized();
            logger.LogInformation("Database initialization completed and marked as ready");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Database initialization was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRITICAL: Background database initialization failed");
            dbInitService.MarkAsFailed(ex);
        }
    }

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("=== STARTING DATABASE INITIALIZATION ===");
        logger.LogInformation("Environment: {Environment}", environment.EnvironmentName);

        // Handle force recreation if needed
        await HandleForceDatabaseRecreation(context, cancellationToken);

        // Initialize database with retry logic
        await InitializeDatabaseWithRetry(context, cancellationToken);

        // Perform seeding
        await SeedDatabaseAsync(scope, cancellationToken);
    }

    private async Task HandleForceDatabaseRecreation(
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        var forceRecreate = Environment.GetEnvironmentVariable("FORCE_DB_RECREATE");
        logger.LogInformation(
            "FORCE_DB_RECREATE environment variable: '{ForceRecreate}'",
            forceRecreate ?? "NULL"
        );

        if (forceRecreate?.ToLower() == "true")
        {
            logger.LogWarning("FORCE_DB_RECREATE=true detected. Recreating database...");
            try
            {
                logger.LogInformation("Attempting to delete database...");
                await context.Database.EnsureDeletedAsync(cancellationToken);
                logger.LogInformation("Database deleted successfully.");
                await Task.Delay(2000, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                logger.LogError(
                    deleteEx,
                    "Failed to delete database: {Message}",
                    deleteEx.Message
                );
            }
        }
    }

    private async Task InitializeDatabaseWithRetry(
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Waiting for PostgreSQL to be fully ready...");
    
        var isProduction = environment.IsProduction();
        var maxRetries = isProduction ? 30 : 15;
        var initialDelayMs = isProduction ? 10000 : 5000;
        var baseRetryDelayMs = isProduction ? 10000 : 8000; 
        await Task.Delay(initialDelayMs, cancellationToken);

        for (int retryCount = 1; retryCount <= maxRetries; retryCount++)
        {
            try
            {
                logger.LogInformation(
                    "Database initialization attempt {RetryCount}/{MaxRetries}...",
                    retryCount,
                    maxRetries
                );

                // Test connection
                await TestDatabaseConnection(context, cancellationToken);

                // Create database and apply migrations
                await CreateDatabaseAndApplyMigrations(context, cancellationToken);

                logger.LogInformation("Database initialization completed successfully!");
                return;
            }
            catch (Exception ex)
            {
                var isConnectionError = ex is NpgsqlException || 
                                       ex.InnerException is NpgsqlException ||
                                       ex.Message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase) ||
                                       ex.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase);

                if (isConnectionError)
                {
                    logger.LogWarning(
                        "Database connection failed (attempt {RetryCount}/{MaxRetries}): {Message}. " +
                        "This is expected if database is still starting up.",
                        retryCount,
                        maxRetries,
                        ex.Message
                    );
                }
                else
                {
                    logger.LogWarning(
                        ex,
                        "Database initialization attempt {RetryCount} failed: {Message}",
                        retryCount,
                        ex.Message
                    );
                }

                if (retryCount < maxRetries)
                {
                   
                    var delayMs = Math.Min(
                        baseRetryDelayMs * (int)Math.Pow(1.2, retryCount - 1),
                        30000
                    );
                    await Task.Delay((int)delayMs, cancellationToken);
                }
                else
                {
                    logger.LogError(
                        ex,
                        "CRITICAL: Cannot initialize database after {MaxRetries} attempts. " +
                        "Total wait time: approximately {TotalWaitMinutes} minutes.",
                        maxRetries,
                        (initialDelayMs + baseRetryDelayMs * maxRetries) / 60000.0
                    );
                    throw;
                }
            }
        }
    }

    private async Task TestDatabaseConnection(
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Testing PostgreSQL server and database...");

        var connectionString = context.Database.GetConnectionString();
       
        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connectionBuilder.Database;
        
        connectionBuilder.Timeout = 10; 
        var serverConnectionString = connectionBuilder.ConnectionString.Replace(
            $"Database={databaseName}",
            "Database=postgres"
        );

        logger.LogInformation(
            "Checking if database '{DatabaseName}' exists on server '{Host}:{Port}'...",
            databaseName,
            connectionBuilder.Host,
            connectionBuilder.Port
        );

        // Connect to postgres database to check if target database exists
        using var serverConnection = new NpgsqlConnection(serverConnectionString);
        
        try
        {
            await serverConnection.OpenAsync(cancellationToken);
            logger.LogInformation("Connected to PostgreSQL server successfully");
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(
                ex,
                "Failed to connect to PostgreSQL server at '{Host}:{Port}'. " +
                "This may indicate the database container is still starting up.",
                connectionBuilder.Host,
                connectionBuilder.Port
            );
            throw;
        }

        // Check if database exists
        using var cmd = new NpgsqlCommand(
            $"SELECT 1 FROM pg_database WHERE datname = '{databaseName}'",
            serverConnection
        );
        var exists = await cmd.ExecuteScalarAsync(cancellationToken);

        if (exists == null)
        {
            logger.LogInformation(
                "Database '{DatabaseName}' does not exist. Creating it...",
                databaseName
            );

            // Create database
            using var createCmd = new NpgsqlCommand(
                $"CREATE DATABASE \"{databaseName}\"",
                serverConnection
            );
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
            logger.LogInformation("Database '{DatabaseName}' created successfully", databaseName);
        }
        else
        {
            logger.LogInformation("Database '{DatabaseName}' already exists", databaseName);
        }

        // Now test connection to the actual database
        await context.Database.OpenConnectionAsync(cancellationToken);
        await context.Database.CloseConnectionAsync();

        logger.LogInformation("PostgreSQL server and database are accessible");
    }

    private async Task CreateDatabaseAndApplyMigrations(
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Checking and applying database migrations...");

        try
        {
            // Check pending migrations first
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
               
                await context.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "42P07")
        {

                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync(cancellationToken);
                }
            
        }
    }

    private async Task SeedDatabaseAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        logger.LogInformation("=== STARTING DATABASE SEEDING ===");


        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verify tables exist
        if (!await VerifyRequiredTablesExist(context, cancellationToken))
        {
            logger.LogError("Required tables don't exist. Skipping seeding.");
            return;
        }

        // Perform seeding with retry
        await SeedWithRetry(scope, cancellationToken);

        logger.LogInformation("Databasen initialization process completed");
    }

    private async Task<bool> VerifyRequiredTablesExist(
        ApplicationDbContext context,
        CancellationToken cancellationToken
    )
    {
        try
        {
            logger.LogInformation("Verifying required tables exist...");

            // Check AspNetRoles table
            await context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM \"AspNetRoles\" LIMIT 1",
                cancellationToken
            );
            logger.LogInformation("AspNetRoles table exists");

            // Check AspNetUsers table
            await context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM \"AspNetUsers\" LIMIT 1",
                cancellationToken
            );
            logger.LogInformation("AspNetUsers table exists");

            logger.LogInformation("All required tables exist - ready for seeding");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify required tables: {Message}", ex.Message);
            return false;
        }
    }

    private async Task SeedWithRetry(IServiceScope scope, CancellationToken cancellationToken)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        const int maxSeedRetries = 5;
        const int seedRetryDelayMs = 5000;

        for (int seedRetryCount = 1; seedRetryCount <= maxSeedRetries; seedRetryCount++)
        {
            try
            {
                logger.LogInformation(
                    "Database seeding attempt {RetryCount}/{MaxRetries}...",
                    seedRetryCount,
                    maxSeedRetries
                );

                if (seedRetryCount > 1)
                {
                    await Task.Delay(2000, cancellationToken);
                }

                await seeder.SeedAsync(cancellationToken);
                logger.LogInformation(" Database seeded successfully!");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Database seeding attempt {RetryCount} failed: {Message}",
                    seedRetryCount,
                    ex.Message
                );

                if (seedRetryCount < maxSeedRetries)
                {
                    logger.LogInformation(
                        "Retrying seeding in {Delay} seconds...",
                        seedRetryDelayMs / 1000
                    );
                    await Task.Delay(seedRetryDelayMs, cancellationToken);
                }
                else
                {
                    logger.LogError(
                        ex,
                        "Database seeding failed after {MaxRetries} attempts. Application will continue but default data may not be available.",
                        maxSeedRetries
                    );
                }
            }
        }
    }
}
