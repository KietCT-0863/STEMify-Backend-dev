using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Users.Queries.GetTeacherProfileCompletion;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Moq;
using Xunit;

namespace Identity.Application.UnitTests.Users.Queries;

public class GetTeacherProfileCompletionQueryHandlerTests
{
    private readonly Mock<ITeacherRepository> _mockTeacherRepository;
    private readonly GetTeacherProfileCompletionQueryHandler _handler;

    public GetTeacherProfileCompletionQueryHandlerTests()
    {
        _mockTeacherRepository = new Mock<ITeacherRepository>();
        _handler = new GetTeacherProfileCompletionQueryHandler(_mockTeacherRepository.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnProfileCompletion_WhenTeacherExists()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacher = Teacher.Create(
            teacherId,
            "teacher@example.com",
            "teacheruser",
            "Jane",
            "Smith",
            "Experienced teacher",
            "Mathematics"
        );

        _mockTeacherRepository
            .Setup(x => x.FindByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher);

        var query = new GetTeacherProfileCompletionQuery { UserId = teacherId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.CompletionPercentage);
        Assert.Equal(5, result.TotalFields);
        Assert.Equal(5, result.CompletedFields);
        Assert.Empty(result.MissingFields);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public async Task Handle_ShouldReturnPartialCompletion_WhenTeacherHasMissingFields()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        var teacher = Teacher.Create(
            teacherId,
            "teacher@example.com",
            "teacheruser",
            "Jane",
            "Smith"
        // Missing bio and specialization
        );

        _mockTeacherRepository
            .Setup(x => x.FindByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacher);

        var query = new GetTeacherProfileCompletionQuery { UserId = teacherId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(60, result.CompletionPercentage); // 3 out of 5 fields = 60%
        Assert.Equal(5, result.TotalFields);
        Assert.Equal(3, result.CompletedFields);
        Assert.Contains("Bio", result.MissingFields);
        Assert.Contains("Specialization", result.MissingFields);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenTeacherDoesNotExist()
    {
        // Arrange
        var teacherId = Guid.NewGuid();
        _mockTeacherRepository
            .Setup(x => x.FindByIdAsync(teacherId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Teacher?)null);

        var query = new GetTeacherProfileCompletionQuery { UserId = teacherId };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );
    }
}
