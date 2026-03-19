using FluentAssertions;
using Identity.Application.Common.Models;
using Xunit;

namespace Identity.Application.UnitTests.Common.Models;

/// <summary>
/// Unit tests for SeedingResult and AggregateSeedingResult
/// Testing domain models and their behavior
/// </summary>
public class SeedingResultTests
{
    public class SeedingResultTests_Success
    {
        [Fact]
        public void Success_WithDefaultParameters_ShouldCreateSuccessfulResult()
        {
            // Act
            var result = SeedingResult.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ItemsSeeded.Should().Be(0);
            result.ItemsSkipped.Should().Be(0);
            result.Messages.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Success_WithParameters_ShouldCreateResultWithCorrectValues()
        {
            // Arrange
            var messages = new List<string> { "Message 1", "Message 2" };

            // Act
            var result = SeedingResult.Success(itemsSeeded: 5, itemsSkipped: 2, messages: messages);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ItemsSeeded.Should().Be(5);
            result.ItemsSkipped.Should().Be(2);
            result.Messages.Should().Equal(messages);
        }
    }

    public class SeedingResultTests_Failure
    {
        [Fact]
        public void Failure_WithErrorMessage_ShouldCreateFailedResult()
        {
            // Arrange
            var errorMessage = "Test error message";

            // Act
            var result = SeedingResult.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be(errorMessage);
            result.ItemsSeeded.Should().Be(0);
            result.ItemsSkipped.Should().Be(0);
            result.Messages.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Failure_WithException_ShouldCreateFailedResultWithExceptionMessage()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = SeedingResult.Failure(exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be(exception.Message);
            result.ItemsSeeded.Should().Be(0);
            result.ItemsSkipped.Should().Be(0);
            result.Messages.Should().NotBeNull().And.BeEmpty();
        }
    }

    public class AggregateSeedingResultTests
    {
        [Fact]
        public void IsSuccess_WithAllSuccessfulResults_ShouldReturnTrue()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 2));
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 3));

            // Act & Assert
            aggregate.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void IsSuccess_WithAnyFailedResult_ShouldReturnFalse()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 2));
            aggregate.AddResult(SeedingResult.Failure("Error"));
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 3));

            // Act & Assert
            aggregate.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void TotalItemsSeeded_ShouldSumAllSeededItems()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 2, itemsSkipped: 1));
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 3, itemsSkipped: 2));
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 5, itemsSkipped: 0));

            // Act & Assert
            aggregate.TotalItemsSeeded.Should().Be(10); // 2 + 3 + 5
        }

        [Fact]
        public void TotalItemsSkipped_ShouldSumAllSkippedItems()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 2, itemsSkipped: 1));
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 3, itemsSkipped: 2));
            aggregate.AddResult(SeedingResult.Success(itemsSeeded: 5, itemsSkipped: 3));

            // Act & Assert
            aggregate.TotalItemsSkipped.Should().Be(6); // 1 + 2 + 3
        }

        [Fact]
        public void AllMessages_ShouldCombineAllMessages()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            aggregate.AddResult(
                SeedingResult.Success(messages: new List<string> { "Message 1", "Message 2" })
            );
            aggregate.AddResult(SeedingResult.Success(messages: new List<string> { "Message 3" }));

            // Act & Assert
            aggregate.AllMessages.Should().Equal("Message 1", "Message 2", "Message 3");
        }

        [Fact]
        public void Errors_ShouldReturnOnlyErrorMessages()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            aggregate.AddResult(SeedingResult.Success());
            aggregate.AddResult(SeedingResult.Failure("Error 1"));
            aggregate.AddResult(SeedingResult.Failure("Error 2"));

            // Act & Assert
            aggregate.Errors.Should().Equal("Error 1", "Error 2");
        }

        [Fact]
        public void AddResult_ShouldAddResultToCollection()
        {
            // Arrange
            var aggregate = new AggregateSeedingResult();
            var result1 = SeedingResult.Success();
            var result2 = SeedingResult.Failure("Error");

            // Act
            aggregate.AddResult(result1);
            aggregate.AddResult(result2);

            // Assert
            aggregate.Results.Should().HaveCount(2);
            aggregate.Results.Should().Contain(result1);
            aggregate.Results.Should().Contain(result2);
        }

        [Fact]
        public void EmptyAggregate_ShouldHaveCorrectDefaults()
        {
            // Arrange & Act
            var aggregate = new AggregateSeedingResult();

            // Assert
            aggregate.IsSuccess.Should().BeTrue(); // No failures means success
            aggregate.TotalItemsSeeded.Should().Be(0);
            aggregate.TotalItemsSkipped.Should().Be(0);
            aggregate.AllMessages.Should().BeEmpty();
            aggregate.Errors.Should().BeEmpty();
            aggregate.Results.Should().BeEmpty();
        }
    }
}
