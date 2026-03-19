using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Users.Queries.GetStudentAge;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.Application.UnitTests.Users.Queries;

public class GetStudentAgeQueryHandlerTests
{
    private readonly Mock<IIdentityUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly GetStudentAgeQueryHandler _handler;

    public GetStudentAgeQueryHandlerTests()
    {
        _mockUnitOfWork = new Mock<IIdentityUnitOfWork>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUserRepository.Object);
        _handler = new GetStudentAgeQueryHandler(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnStudentAge_WhenStudentExists()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var dateOfBirth = new DateTime(2000, 1, 1); // Student born in 2000
        var student = Student.Create(
            studentId,
            "test@example.com",
            "testuser",
            "John",
            "Doe",
            dateOfBirth,
            "Test bio",
            "Computer Science"
        );

        _mockUserRepository
            .Setup(x => x.GetStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var query = new GetStudentAgeQuery { UserId = studentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(student.GetAge(), result);
        Assert.True(result > 0);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenStudentNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockUserRepository
            .Setup(x => x.GetStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Student?)null);

        var query = new GetStudentAgeQuery { UserId = studentId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );

        Assert.Equal($"Student with ID {studentId} not found", exception.Message);
    }

    [Fact]
    public async Task Handle_ShouldUseStudentGetAgeMethod()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var dateOfBirth = new DateTime(1995, 6, 15); // Specific date for age calculation
        var student = Student.Create(
            studentId,
            "test@example.com",
            "testuser",
            "Jane",
            "Smith",
            dateOfBirth,
            "Test bio",
            "Mathematics"
        );

        _mockUserRepository
            .Setup(x => x.GetStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        var query = new GetStudentAgeQuery { UserId = studentId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        // Verify that the result matches what Student.GetAge() would return
        var expectedAge = student.GetAge();
        Assert.Equal(expectedAge, result);

        // Verify the repository was called correctly
        _mockUserRepository.Verify(
            x => x.GetStudentAsync(studentId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
