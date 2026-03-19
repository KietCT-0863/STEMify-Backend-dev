using Identity.Application.Common.Interfaces.Repositories;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Domain.UnitTests.UnitOfWork;

/// <summary>
/// Unit tests for IdentityUnitOfWork with dependency injection
/// </summary>
[TestFixture]
public class IdentityUnitOfWorkTests
{
    [Test]
    public void Constructor_WithValidDependencies_ShouldInitializeRepositories()
    {
        // Arrange
        var mockContext = CreateMockDbContext();
        var mockUserRepository = new Mock<IUserRepository>();
        var mockStudentRepository = new Mock<IStudentProfileRepository>();
        var mockTeacherRepository = new Mock<ITeacherProfileRepository>();

        // Act
        var unitOfWork = new IdentityUnitOfWork(
            mockContext,
            mockUserRepository.Object,
            mockStudentRepository.Object,
            mockTeacherRepository.Object
        );

        // Assert
        Assert.That(unitOfWork.Users, Is.Not.Null);
        Assert.That(unitOfWork.StudentProfiles, Is.Not.Null);
        Assert.That(unitOfWork.TeacherProfiles, Is.Not.Null);

        Assert.That(unitOfWork.Users, Is.SameAs(mockUserRepository.Object));
        Assert.That(unitOfWork.StudentProfiles, Is.SameAs(mockStudentRepository.Object));
        Assert.That(unitOfWork.TeacherProfiles, Is.SameAs(mockTeacherRepository.Object));
    }

    [Test]
    public void Repositories_ShouldBeInjectedInstances_NotNewInstances()
    {
        // Arrange
        var mockContext = CreateMockDbContext();
        var mockUserRepository = new Mock<IUserRepository>();
        var mockStudentRepository = new Mock<IStudentProfileRepository>();
        var mockTeacherRepository = new Mock<ITeacherProfileRepository>();

        var unitOfWork = new IdentityUnitOfWork(
            mockContext,
            mockUserRepository.Object,
            mockStudentRepository.Object,
            mockTeacherRepository.Object
        );

        // Act & Assert - Multiple calls should return same instance
        Assert.That(unitOfWork.Users, Is.SameAs(unitOfWork.Users));
        Assert.That(unitOfWork.StudentProfiles, Is.SameAs(unitOfWork.StudentProfiles));
        Assert.That(unitOfWork.TeacherProfiles, Is.SameAs(unitOfWork.TeacherProfiles));
    }

    [Test]
    public void UnitOfWork_WithMockedRepositories_ShouldEnableTestingBusinessLogic()
    {
        // Arrange
        var mockContext = CreateMockDbContext();
        var mockUserRepository = new Mock<IUserRepository>();
        var mockStudentRepository = new Mock<IStudentProfileRepository>();
        var mockTeacherRepository = new Mock<ITeacherProfileRepository>();

        // Setup mock behavior
        var testGuid = Guid.NewGuid();
        mockUserRepository
            .Setup(x => x.FindByIdAsync(testGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Identity.Domain.Entities.User?)null);

        var unitOfWork = new IdentityUnitOfWork(
            mockContext,
            mockUserRepository.Object,
            mockStudentRepository.Object,
            mockTeacherRepository.Object
        );

        // Act
        var userRepository = unitOfWork.Users;

        // Assert
        Assert.That(userRepository, Is.Not.Null);
        // Can verify mock interactions
        mockUserRepository.Verify(
            x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    /// <summary>
    /// Creates a mock ApplicationDbContext for testing
    /// </summary>
    /// <returns>Mocked ApplicationDbContext</returns>
    private static ApplicationDbContext CreateMockDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}

/// <summary>
/// Example of how the old manual instantiation approach was difficult to test
/// </summary>
public class LegacyUnitOfWorkExample
{
    // OLD APPROACH - Hard to test due to manual instantiation
    /*
    public class OldIdentityUnitOfWork : UnitOfWork<ApplicationDbContext>
    {
        public IUserRepository Users => _users ??= new UserRepository(Context, this);
        
        // Problem: Cannot mock UserRepository in unit tests
        // Problem: Tight coupling to concrete implementation
        // Problem: Violates Dependency Inversion Principle
    }
    */

    //  NEW APPROACH - Easy to test with dependency injection
    // See IdentityUnitOfWorkTests above for examples
}
