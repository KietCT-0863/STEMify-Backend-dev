using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Domain.UnitTests.Entities;

[TestFixture]
public class UserInvariantsTests
{
    private static readonly Email TestEmail = Email.Create("test@example.com");
    private static readonly UserName TestUserName = UserName.Create("testuser");
    private static readonly string TestFirstName = "Test";
    private static readonly string TestLastName = "User";

    #region Identity Invariants

    [Test]
    public void User_Id_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.CreateTeacher(userId, TestEmail, TestUserName);

        // Act - Try to change ID through reflection (should not be possible)
        var idProperty = typeof(User).GetProperty("Id");
        var hasPublicSetter = idProperty?.SetMethod?.IsPublic ?? false;

        // Assert
        user.Id.Should().Be(userId);
        hasPublicSetter.Should().BeFalse("User ID should not have public setter");
    }

    [Test]
    public void User_Email_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);

        // Act - Try to change Email through reflection
        var emailProperty = typeof(User).GetProperty("Email");
        var hasPublicSetter = emailProperty?.SetMethod?.IsPublic ?? false;

        // Assert
        user.Email.Should().Be(TestEmail);
        hasPublicSetter.Should().BeFalse("User Email should not have public setter");
    }

    [Test]
    public void User_UserName_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var user = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);

        // Act - Try to change UserName through reflection
        var userNameProperty = typeof(User).GetProperty("UserName");
        var hasPublicSetter = userNameProperty?.SetMethod?.IsPublic ?? false;

        // Assert
        user.UserName.Should().Be(TestUserName);
        hasPublicSetter.Should().BeFalse("User UserName should not have public setter");
    }

    #endregion

    #region Status Transition Invariants

    [Test]
    public void User_StatusTransition_FromPendingToActive_ShouldBeValid()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        user.Status.Should().Be(UserStatus.Pending);

        // Act
        user.Activate();

        // Assert
        user.Status.Should().Be(UserStatus.Active);
    }

    [Test]
    public void User_StatusTransition_FromActiveToActive_ShouldNotChangeTimestamp()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        var originalUpdatedAt = user.UpdatedAt;

        // Act
        user.Activate(); // Try to activate again

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().Be(originalUpdatedAt); // Should not change
    }

    [Test]
    public void User_Status_ShouldNotAllowDirectManipulation()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);

        // Act - Try to change Status through reflection
        var statusProperty = typeof(User).GetProperty("Status");
        var hasPublicSetter = statusProperty?.SetMethod?.IsPublic ?? false;

        // Assert
        hasPublicSetter.Should().BeFalse("User Status should not have public setter");
    }

    #endregion

    #region Role Invariants

    [Test]
    public void User_Role_ShouldMatchFactoryMethod()
    {
        // Arrange & Act
        var teacher = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var student = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        var admin = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);
        var staff = User.CreateStaff(Guid.NewGuid(), TestEmail, TestUserName);

        // Assert
        teacher.Role.Should().Be(UserRole.Teacher);
        student.Role.Should().Be(UserRole.Student);
        admin.Role.Should().Be(UserRole.Admin);
        staff.Role.Should().Be(UserRole.Staff);
    }

    [Test]
    public void User_Role_ShouldNotAllowDirectManipulation()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);

        // Act - Try to change Role through reflection
        var roleProperty = typeof(User).GetProperty("Role");
        var hasPublicSetter = roleProperty?.SetMethod?.IsPublic ?? false;

        // Assert
        user.Role.Should().Be(UserRole.Teacher);
        hasPublicSetter.Should().BeFalse("User Role should not have public setter");
    }

    #endregion

    #region Timestamp Invariants

    [Test]
    public void User_CreatedAt_ShouldBeImmutableAfterCreation()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        var originalCreatedAt = user.CreatedAt;

        // Act - Perform various operations
        user.Activate();
        user.Login();

        // Try to change CreatedAt through reflection
        var createdAtProperty = typeof(User).GetProperty("CreatedAt");
        var hasPublicSetter = createdAtProperty?.SetMethod?.IsPublic ?? false;

        // Assert
        user.CreatedAt.Should().Be(originalCreatedAt);
        hasPublicSetter.Should().BeFalse("User CreatedAt should not have public setter");
    }

    [Test]
    public void User_UpdatedAt_ShouldChangeOnlyWhenStateChanges()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var originalUpdatedAt = user.UpdatedAt;

        // Act - Operations that should update timestamp
        user.Activate();
        var updatedAtAfterActivation = user.UpdatedAt;

        user.Login();
        var updatedAtAfterLogin = user.UpdatedAt;

        // Assert
        updatedAtAfterActivation.Should().BeAfter(originalUpdatedAt);
        updatedAtAfterLogin.Should().BeAfter(updatedAtAfterActivation);
    }

    [Test]
    public void User_CreatedAt_ShouldBeBeforeOrEqualToUpdatedAt()
    {
        // Arrange
        var user = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);

        // Act - Perform state changes
        user.Activate();
        user.Login();

        // Assert
        user.CreatedAt.Should().BeOnOrBefore(user.UpdatedAt);
    }

    #endregion

    #region Profile Invariants

    [Test]
    public void User_TeacherProfile_ShouldOnlyExistForTeachers()
    {
        // Arrange
        var teacher = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var student = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);

        // Act
        teacher.CreateTeacherProfile(TestFirstName, TestLastName, "Bio", "Specialization");

        // Assert
        teacher.TeacherProfile.Should().NotBeNull();
        student.TeacherProfile.Should().BeNull();
    }

    [Test]
    public void User_StudentProfile_ShouldOnlyExistForStudents()
    {
        // Arrange
        var student = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        var teacher = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var dateOfBirth = DateTime.Today.AddYears(-20);

        // Act
        student.CreateStudentProfile(TestFirstName, TestLastName, dateOfBirth, "Bio", "Major");

        // Assert
        student.StudentProfile.Should().NotBeNull();
        teacher.StudentProfile.Should().BeNull();
    }

    [Test]
    public void User_NonTeacher_ShouldNotCreateTeacherProfile()
    {
        // Arrange
        var student = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        var admin = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            student.CreateTeacherProfile(TestFirstName, TestLastName, "Bio", "Specialization")
        );

        Assert.Throws<InvalidOperationException>(() =>
            admin.CreateTeacherProfile(TestFirstName, TestLastName, "Bio", "Specialization")
        );
    }

    [Test]
    public void User_NonStudent_ShouldNotCreateStudentProfile()
    {
        // Arrange
        var teacher = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var admin = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);
        var dateOfBirth = DateTime.Today.AddYears(-20);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            teacher.CreateStudentProfile(TestFirstName, TestLastName, dateOfBirth, "Bio", "Major")
        );

        Assert.Throws<InvalidOperationException>(() =>
            admin.CreateStudentProfile(TestFirstName, TestLastName, dateOfBirth, "Bio", "Major")
        );
    }

    #endregion

    #region Login Invariants

    [Test]
    public void User_Login_ShouldRequireActiveStatus()
    {
        // Arrange
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);
        // User is Pending by default

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => user.Login());
        user.Status.Should().Be(UserStatus.Pending);
    }

    [Test]
    public void User_Login_ShouldUpdateLastLoginAt()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        user.LastLoginAt.Should().BeNull();

        // Act
        user.Login();

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void User_CanLogin_ShouldRequireActiveStatusAndConfirmedEmail()
    {
        // Arrange
        var user = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);

        // Act & Assert - Not active
        user.CanLogin().Should().BeFalse();

        // Activate but email not confirmed
        user.Activate();
        user.CanLogin().Should().BeFalse();

        // Email confirmed but simulate confirmed email
        typeof(User).GetProperty("EmailConfirmedAt")!.SetValue(user, DateTime.UtcNow);
        user.CanLogin().Should().BeTrue();
    }

    #endregion

    #region Domain Events Invariants

    [Test]
    public void User_Creation_ShouldGenerateUserCreatedEvent()
    {
        // Arrange & Act
        var user = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);

        // Assert
        user.DomainEvents.Should().HaveCount(1);
        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserCreatedEvent>();

        var userCreatedEvent = (UserCreatedEvent)domainEvent;
        userCreatedEvent.UserId.Should().Be(user.Id);
        userCreatedEvent.Email.Should().Be(TestEmail.Value);
        userCreatedEvent.UserName.Should().Be(TestUserName.Value);
        userCreatedEvent.Role.Should().Be(UserRole.Student.ToString());
    }

    [Test]
    public void User_Activation_ShouldGenerateUserActivatedEvent()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.DomainEvents.ToList().Clear(); // Clear creation event

        // Act
        user.Activate("admin123");

        // Assert
        user.DomainEvents.Should().HaveCount(1);
        var domainEvent = user.DomainEvents.First();
        domainEvent.Should().BeOfType<UserActivatedEvent>();

        var userActivatedEvent = (UserActivatedEvent)domainEvent;
        userActivatedEvent.UserId.Should().Be(user.Id);
        userActivatedEvent.Email.Should().Be(TestEmail.Value);
        userActivatedEvent.ActivatedBy.Should().Be("admin123");
    }

    [Test]
    public void User_Login_ShouldNotGenerateDomainEvent()
    {
        // Arrange
        var user = User.CreateAdmin(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();
        user.DomainEvents.ToList().Clear(); // Clear previous events

        // Act
        user.Login("192.168.1.1", "Mozilla/5.0");

        // Assert
        // Login should not add domain events (UserLoggedInEvent is application concern)
        user.DomainEvents.Should().HaveCount(0);
    }

    [Test]
    public void User_DomainEvents_ShouldAccumulateMultipleEvents()
    {
        // Arrange
        var user = User.CreateStaff(Guid.NewGuid(), TestEmail, TestUserName);

        // Act - Perform multiple operations
        user.Activate("admin123");
        user.Login("192.168.1.1", "Mozilla/5.0");

        // Assert
        user.DomainEvents.Should().HaveCount(2); // Created + Activated (Login doesn't add domain events)
        user.DomainEvents.Should().Contain(e => e is UserCreatedEvent);
        user.DomainEvents.Should().Contain(e => e is UserActivatedEvent);
    }

    #endregion

    #region Aggregate Root Invariants

    [Test]
    public void User_ShouldBeAggregateRoot()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);

        // Act & Assert - User should have domain events collection
        user.DomainEvents.Should().NotBeNull();
        user.DomainEvents.Should().BeAssignableTo<IReadOnlyList<object>>();
    }

    [Test]
    public void User_ProfilesRelationship_ShouldMaintainReferentialIntegrity()
    {
        // Arrange
        var teacher = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        var student = User.CreateStudent(Guid.NewGuid(), TestEmail, TestUserName);

        // Act
        teacher.CreateTeacherProfile(TestFirstName, TestLastName, "Bio", "Specialization");
        student.CreateStudentProfile(
            TestFirstName,
            TestLastName,
            DateTime.Today.AddYears(-20),
            "Bio",
            "Major"
        );

        // Assert
        teacher.TeacherProfile.Should().NotBeNull();
        teacher.TeacherProfile!.Id.Should().Be(teacher.Id);
        teacher.StudentProfile.Should().BeNull();

        student.StudentProfile.Should().NotBeNull();
        student.StudentProfile!.Id.Should().Be(student.Id);
        student.TeacherProfile.Should().BeNull();
    }

    #endregion

    #region Concurrency Invariants

    [Test]
    public void User_ConcurrentOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var user = User.CreateTeacher(Guid.NewGuid(), TestEmail, TestUserName);
        user.Activate();

        // Act - Simulate concurrent operations
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => user.Login($"192.168.1.{i}", "Mozilla/5.0")));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - User state should remain consistent
        user.Status.Should().Be(UserStatus.Active);
        user.LastLoginAt.Should().NotBeNull();
        // Domain events should have accumulated (but exact count may vary due to concurrency)
        user.DomainEvents.Should().NotBeEmpty();
    }

    #endregion
}
