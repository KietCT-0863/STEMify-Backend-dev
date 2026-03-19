using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Domain.UnitTests.Entities;

[TestFixture]
public class UserTests
{
    private static readonly Email TestEmail = Email.Create("test@example.com");
    private static readonly UserName TestUserName = UserName.Create("testuser");
    private static readonly string TestFirstName = "Nguyễn Văn";
    private static readonly string TestLastName = "Test";

    [Test]
    public void CreateTeacher_WithValidData_ShouldCreateTeacherWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var user = User.CreateTeacher(userId, TestEmail, TestUserName);

        // Assert
        user.Id.Should().Be(userId);
        user.Email.Should().Be(TestEmail);
        user.UserName.Should().Be(TestUserName);
        user.Role.Should().Be(UserRole.Teacher);
        user.Status.Should().Be(UserStatus.Pending);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastLoginAt.Should().BeNull();
        user.EmailConfirmedAt.Should().BeNull();
    }

    [Test]
    public void CreateTeacher_ShouldAddUserCreatedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var user = User.CreateTeacher(userId, TestEmail, TestUserName);

        // Assert
        user.DomainEvents.Should().HaveCount(1);
        var domainEvent = user.DomainEvents.First() as UserCreatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(userId);
        domainEvent.Email.Should().Be(TestEmail.Value);
        domainEvent.UserName.Should().Be(TestUserName.Value);
        domainEvent.Role.Should().Be(UserRole.Teacher.ToString());
    }

    [Test]
    public void CreateStudent_WithValidData_ShouldCreateStudentWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var user = User.CreateStudent(userId, TestEmail, TestUserName);

        // Assert
        user.Id.Should().Be(userId);
        user.Role.Should().Be(UserRole.Student);
        user.Status.Should().Be(UserStatus.Pending);
    }

    [Test]
    public void CreateStaff_WithValidData_ShouldCreateStaffWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var user = User.CreateStaff(userId, TestEmail, TestUserName);

        // Assert
        user.Role.Should().Be(UserRole.Staff);
    }

    [Test]
    public void CreateAdmin_WithValidData_ShouldCreateAdminWithCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var user = User.CreateAdmin(userId, TestEmail, TestUserName);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
    }

    [Test]
    public void Activate_FromPendingStatus_ShouldActivateUser()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var activatedBy = "admin123";

        // Act
        user.Activate(activatedBy);

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Activate_ShouldAddUserActivatedEvent()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var activatedBy = "admin123";

        // Act
        user.Activate(activatedBy);

        // Assert
        user.DomainEvents.Should().HaveCount(2); // UserCreated + UserActivated
        var activatedEvent = user.DomainEvents.OfType<UserActivatedEvent>().First();
        activatedEvent.UserId.Should().Be(user.Id);
        activatedEvent.Email.Should().Be(TestEmail.Value);
        activatedEvent.ActivatedBy.Should().Be(activatedBy);
    }

    [Test]
    public void Activate_WhenAlreadyActive_ShouldBeIdempotent()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        var originalUpdateTime = user.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        user.Activate();

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().Be(originalUpdateTime); // Should not change
        user.DomainEvents.OfType<UserActivatedEvent>().Should().HaveCount(1); // Only one event
    }

    [Test]
    public void Activate_WhenDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        // Simulate deleted status (would normally be set through business method)
        typeof(User).GetProperty("Status")!.SetValue(user, UserStatus.Deleted);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => user.Activate());
        exception.Message.Should().Contain("Cannot activate deleted user");
    }

    [Test]
    public void Login_WhenActive_ShouldUpdateLastLoginTime()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        // Act
        user.Login(ipAddress, userAgent);

        // Assert
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Login_ShouldNotAddDomainEvent()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var initialEventCount = user.DomainEvents.Count;

        // Act
        user.Login(ipAddress, userAgent);

        // Assert
        // Login should not add domain events (UserLoggedInEvent is application concern)
        user.DomainEvents.Count.Should().Be(initialEventCount);
    }

    [Test]
    public void Login_WhenNotActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        // User is Pending by default

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => user.Login());
        exception.Message.Should().Contain("Only active users can login");
    }

    [Test]
    public void CreateTeacherProfile_WhenTeacherRole_ShouldCreateProfile()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var bio = "Experienced teacher";
        var specialization = "Mathematics";

        // Act
        user.CreateTeacherProfile(TestFirstName, TestLastName, bio, specialization);

        // Assert
        user.TeacherProfile.Should().NotBeNull();
        user.TeacherProfile!.FirstName.Should().Be(TestFirstName);
        user.TeacherProfile.LastName.Should().Be(TestLastName);
        user.TeacherProfile.FullName.Should().Be($"{TestFirstName} {TestLastName}");
        user.TeacherProfile.Bio.Should().Be(bio);
        user.TeacherProfile.Specialization.Should().Be(specialization);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void CreateTeacherProfile_ShouldAddProfileUpdatedEvent()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);

        // Act
        user.CreateTeacherProfile(TestFirstName, TestLastName);

        // Assert
        var profileEvent = user.DomainEvents.OfType<UserProfileUpdatedEvent>().First();
        profileEvent.UserId.Should().Be(user.Id);
        profileEvent.ProfileType.Should().Be(nameof(TeacherProfile));
        profileEvent.UpdatedFields.Should().Be("Created");
    }

    [Test]
    public void CreateTeacherProfile_WhenNotTeacher_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            user.CreateTeacherProfile(TestFirstName, TestLastName)
        );
        exception.Message.Should().Contain("Only teachers can create teacher profile");
    }

    [Test]
    public void CreateTeacherProfile_WhenProfileExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.CreateTeacherProfile(TestFirstName, TestLastName);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            user.CreateTeacherProfile(TestFirstName, TestLastName)
        );
        exception.Message.Should().Contain("Teacher profile already exists");
    }

    [Test]
    public void CreateStudentProfile_WhenStudentRole_ShouldCreateProfile()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        var dateOfBirth = DateTime.Today.AddYears(-20);
        var bio = "Enthusiastic student";
        var major = "Computer Science";

        // Act
        user.CreateStudentProfile(TestFirstName, TestLastName, dateOfBirth, bio, major);

        // Assert
        user.StudentProfile.Should().NotBeNull();
        user.StudentProfile!.FirstName.Should().Be(TestFirstName);
        user.StudentProfile.LastName.Should().Be(TestLastName);
        user.StudentProfile.FullName.Should().Be($"{TestFirstName} {TestLastName}");
        user.StudentProfile.DateOfBirth.Should().Be(dateOfBirth);
        user.StudentProfile.Bio.Should().Be(bio);
        user.StudentProfile.Major.Should().Be(major);
    }

    [Test]
    public void CreateStudentProfile_WhenNotStudent_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var dateOfBirth = DateTime.Today.AddYears(-20);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            user.CreateStudentProfile(TestFirstName, TestLastName, dateOfBirth)
        );
        exception.Message.Should().Contain("Only students can create student profile");
    }

    [Test]
    public void IsActive_WhenStatusActive_ShouldReturnTrue()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();

        // Act & Assert
        user.IsActive().Should().BeTrue();
    }

    [Test]
    public void IsActive_WhenStatusNotActive_ShouldReturnFalse()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        // Status is Pending by default

        // Act & Assert
        user.IsActive().Should().BeFalse();
    }

    [Test]
    public void IsEmailConfirmed_WhenEmailConfirmedAtIsNull_ShouldReturnFalse()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);

        // Act & Assert
        user.IsEmailConfirmed().Should().BeFalse();
    }

    [Test]
    public void CanLogin_WhenActiveAndEmailConfirmed_ShouldReturnTrue()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        // Simulate email confirmation
        typeof(User).GetProperty("EmailConfirmedAt")!.SetValue(user, DateTime.UtcNow);

        // Act & Assert
        user.CanLogin().Should().BeTrue();
    }

    [Test]
    public void CanLogin_WhenNotActive_ShouldReturnFalse()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        typeof(User).GetProperty("EmailConfirmedAt")!.SetValue(user, DateTime.UtcNow);

        // Act & Assert
        user.CanLogin().Should().BeFalse();
    }

    [Test]
    public void CanLogin_WhenEmailNotConfirmed_ShouldReturnFalse()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();

        // Act & Assert
        user.CanLogin().Should().BeFalse();
    }
}
