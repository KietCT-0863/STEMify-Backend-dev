using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Users.Queries.GetStudentProfileCompletion;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Xunit;

namespace Identity.Application.UnitTests.Users.Queries;

public class GetStudentProfileCompletionQueryHandlerTests
{
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly GetStudentProfileCompletionQueryHandler _handler;

    public GetStudentProfileCompletionQueryHandlerTests()
    {
        _mockStudentRepository = new Mock<IStudentRepository>();
        _handler = new GetStudentProfileCompletionQueryHandler(_mockStudentRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProfileCompletion_WhenStudentExists()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = Student.Create(
            studentId,
            "test@example.com",
            "testuser",
            "John",
            "Doe",
            new DateTime(2000, 1, 1),
            "Test bio",
            "Computer Science"
        );

        _mockStudentRepository
            .Setup(x => x.FindByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var query = new GetStudentProfileCompletionQuery { UserId = studentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.CompletionPercentage);
        Assert.Equal(6, result.TotalFields);
        Assert.Equal(6, result.CompletedFields);
        Assert.Empty(result.MissingFields);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public async Task Handle_ShouldReturnPartialCompletion_WhenStudentHasMissingFields()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = Student.Create(
            studentId,
            "test@example.com",
            "testuser",
            "John",
            "Doe",
            new DateTime(2000, 1, 1)
        // Missing bio and major
        );

        _mockStudentRepository
            .Setup(x => x.FindByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var query = new GetStudentProfileCompletionQuery { UserId = studentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(67, result.CompletionPercentage); // 4 out of 6 fields = 66.67% rounded to 67%
        Assert.Equal(6, result.TotalFields);
        Assert.Equal(4, result.CompletedFields);
        Assert.Contains("Bio", result.MissingFields);
        Assert.Contains("Major", result.MissingFields);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenStudentDoesNotExist()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockStudentRepository
            .Setup(x => x.FindByIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var query = new GetStudentProfileCompletionQuery { UserId = studentId };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );
    }
}
