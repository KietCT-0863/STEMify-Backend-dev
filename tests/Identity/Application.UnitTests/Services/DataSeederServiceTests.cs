using FluentAssertions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Identity.Application.UnitTests.Services;

/// <summary>
/// Unit tests for DataSeederService
/// Testing application logic in isolation
/// </summary>
public class DataSeederServiceTests
{
    private readonly Mock<ILogger<DataSeederService>> _mockLogger;
    private readonly List<Mock<ISeedingStrategy>> _mockStrategies;
    private readonly DataSeederService _sut;

    public DataSeederServiceTests()
    {
        _mockLogger = new Mock<ILogger<DataSeederService>>();
        _mockStrategies = new List<Mock<ISeedingStrategy>>();

        // Create some mock strategies with different orders
        var strategy1 = new Mock<ISeedingStrategy>();
        strategy1.Setup(s => s.Order).Returns(1);
        _mockStrategies.Add(strategy1);

        var strategy2 = new Mock<ISeedingStrategy>();
        strategy2.Setup(s => s.Order).Returns(2);
        _mockStrategies.Add(strategy2);

        var strategy3 = new Mock<ISeedingStrategy>();
        strategy3.Setup(s => s.Order).Returns(3);
        _mockStrategies.Add(strategy3);

        var strategies = _mockStrategies.Select(m => m.Object);
        _sut = new DataSeederService(strategies, _mockLogger.Object);
    }

    [Fact]
    public async Task SeedAsync_ShouldExecuteStrategiesInOrder()
    {
        // Arrange
        var executionOrder = new List<int>();

        foreach (var mockStrategy in _mockStrategies)
        {
            var order = mockStrategy.Object.Order;
            mockStrategy
                .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback(() => executionOrder.Add(order));
        }

        // Act
        await _sut.SeedAsync();

        // Assert
        executionOrder.Should().BeInAscendingOrder();
        executionOrder.Should().Equal(1, 2, 3);

        // Verify all strategies were called
        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task SeedAsync_ShouldLogStartAndCompletion()
    {
        // Arrange
        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy
                .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        await _sut.SeedAsync();

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Starting application data seeding")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Data seeding completed successfully")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SeedAsync_WhenStrategyFails_ShouldLogErrorAndContinue()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // First strategy succeeds
        _mockStrategies[0]
            .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Second strategy throws exception
        _mockStrategies[1]
            .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Third strategy succeeds
        _mockStrategies[2]
            .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAsync();

        // Assert
        // Verify error was logged
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );

        // Verify all strategies were still called
        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    [Fact]
    public async Task SeedAsync_WithCancellationToken_ShouldPassToStrategies()
    {
        // Arrange
        var cancellationToken = new CancellationToken();

        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy
                .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        await _sut.SeedAsync(cancellationToken);

        // Assert
        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy.Verify(s => s.SeedAsync(cancellationToken), Times.Once);
        }
    }

    [Fact]
    public async Task SeedSampleDataAsync_ShouldDelegateToSeedAsync()
    {
        // Arrange
        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy
                .Setup(s => s.SeedAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        await _sut.SeedSampleDataAsync();

        // Assert
        foreach (var mockStrategy in _mockStrategies)
        {
            mockStrategy.Verify(s => s.SeedAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Starting sample data seeding")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SeedAsync_WithNoStrategies_ShouldCompleteSuccessfully()
    {
        // Arrange
        var emptySeeder = new DataSeederService(
            Enumerable.Empty<ISeedingStrategy>(),
            _mockLogger.Object
        );

        // Act & Assert
        await emptySeeder.SeedAsync();

        // Should log start and completion without errors
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Starting application data seeding")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
