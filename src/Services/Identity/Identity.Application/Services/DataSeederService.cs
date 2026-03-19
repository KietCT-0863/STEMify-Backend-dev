using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

/// <summary>
/// Application service for coordinating data seeding operations
/// Following clean architecture - application services belong in Application layer
/// </summary>
public class DataSeederService : IDataSeeder
{
    private readonly IEnumerable<ISeedingStrategy> _seedingStrategies;
    private readonly ILogger<DataSeederService> _logger;

    public DataSeederService(
        IEnumerable<ISeedingStrategy> seedingStrategies,
        ILogger<DataSeederService> logger
    )
    {
        _seedingStrategies = seedingStrategies;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting application data seeding...");

        var result = new AggregateSeedingResult();

        try
        {
            // Execute seeding strategies in order
            var orderedStrategies = _seedingStrategies.OrderBy(s => s.Order);

            foreach (var strategy in orderedStrategies)
            {
                _logger.LogInformation(
                    "Executing seeding strategy: {StrategyType}",
                    strategy.GetType().Name
                );

                try
                {
                    await strategy.SeedAsync(cancellationToken);
                    result.AddResult(
                        SeedingResult.Success(
                            messages: new List<string>
                            {
                                $"{strategy.GetType().Name} completed successfully",
                            }
                        )
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Seeding strategy {StrategyType} failed: {Message}",
                        strategy.GetType().Name,
                        ex.Message
                    );

                    result.AddResult(
                        SeedingResult.Failure($"{strategy.GetType().Name} failed: {ex.Message}")
                    );
                }
            }

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Data seeding completed successfully! "
                        + "Items seeded: {ItemsSeeded}, Items skipped: {ItemsSkipped}",
                    result.TotalItemsSeeded,
                    result.TotalItemsSkipped
                );
            }
            else
            {
                _logger.LogError(
                    "Data seeding completed with errors: {Errors}",
                    string.Join(", ", result.Errors)
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during data seeding: {Message}", ex.Message);
            throw;
        }
    }

    public async Task SeedSampleDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sample data seeding for development...");

        await SeedAsync(cancellationToken);

        _logger.LogInformation("Sample data seeding completed!");
    }
}
