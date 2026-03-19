using FluentAssertions;
using Identity.Domain.Events;

namespace Domain.UnitTests.Events;

[TestFixture]
public class DomainEventsTests
{
    [Test]
    public void UserCreatedEvent_Create_ShouldSetCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var userName = "testuser";
        var role = "Teacher";
        var createdAt = DateTime.UtcNow;

        // Act
        var domainEvent = new UserCreatedEvent(userId, email, userName, role, createdAt);

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Email.Should().Be(email);
        domainEvent.UserName.Should().Be(userName);
        domainEvent.Role.Should().Be(role);
        domainEvent.CreatedAt.Should().Be(createdAt);
    }

    [Test]
    public void UserActivatedEvent_Create_ShouldSetCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var activatedAt = DateTime.UtcNow;
        var activatedBy = "admin123";

        // Act
        var domainEvent = new UserActivatedEvent(userId, email, activatedAt, activatedBy);

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Email.Should().Be(email);
        domainEvent.ActivatedBy.Should().Be(activatedBy);
        domainEvent.ActivatedAt.Should().Be(activatedAt);
    }

    [Test]
    public void UserActivatedEvent_CreateWithoutActivatedBy_ShouldUseDefaultNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var activatedAt = DateTime.UtcNow;

        // Act
        var domainEvent = new UserActivatedEvent(userId, email, activatedAt);

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Email.Should().Be(email);
        domainEvent.ActivatedBy.Should().BeNull();
        domainEvent.ActivatedAt.Should().Be(activatedAt);
    }

    [Test]
    public void UserProfileUpdatedEvent_Create_ShouldSetCorrectProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profileType = "TeacherProfile";
        var updatedFields = "Bio,Specialization";
        var updatedAt = DateTime.UtcNow;

        // Act
        var domainEvent = new UserProfileUpdatedEvent(
            userId,
            profileType,
            updatedFields,
            updatedAt
        );

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.ProfileType.Should().Be(profileType);
        domainEvent.UpdatedFields.Should().Be(updatedFields);
        domainEvent.UpdatedAt.Should().Be(updatedAt);
    }

    [Test]
    public void UserProfileUpdatedEvent_CreateWithoutUpdatedBy_ShouldUseNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profileType = "StudentProfile";
        var updatedFields = "Bio,Major";
        var updatedAt = DateTime.UtcNow;

        // Act
        var domainEvent = new UserProfileUpdatedEvent(
            userId,
            profileType,
            updatedFields,
            updatedAt
        );

        // Assert
        domainEvent.UserId.Should().Be(userId);
        domainEvent.ProfileType.Should().Be(profileType);
        domainEvent.UpdatedFields.Should().Be(updatedFields);
        domainEvent.UpdatedAt.Should().Be(updatedAt);
        domainEvent.UpdatedBy.Should().BeNull();
    }

    [Test]
    public void DomainEvents_ShouldBeRecords()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userName = "testuser";
        var email = "test@example.com";
        var role = "Teacher";
        var createdAt = DateTime.UtcNow;

        // Act
        var event1 = new UserCreatedEvent(userId, userName, email, role, createdAt);
        var event2 = new UserCreatedEvent(userId, userName, email, role, createdAt);

        // Assert - Records should have value equality based on properties (except auto-generated Ids)
        event1.UserId.Should().Be(event2.UserId);
        event1.UserName.Should().Be(event2.UserName);
        event1.Email.Should().Be(event2.Email);
        event1.Role.Should().Be(event2.Role);
        event1.CreatedAt.Should().Be(event2.CreatedAt);

        // IDs and OccurredOn will be different due to auto-generation
        event1.Id.Should().NotBe(event2.Id);
        event1.OccurredOn.Should().NotBe(event2.OccurredOn);
    }

    [Test]
    public void DomainEvents_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var event1 = new UserCreatedEvent(
            Guid.NewGuid(),
            "test1@example.com",
            "testuser1",
            "Teacher",
            createdAt
        );
        var event2 = new UserCreatedEvent(
            Guid.NewGuid(),
            "test2@example.com",
            "testuser2",
            "Student",
            createdAt
        );

        // Act & Assert
        event1.Should().NotBe(event2);
    }

    [Test]
    public void UserCreatedEvent_ShouldImplementINotification()
    {
        // Arrange & Act
        var domainEvent = new UserCreatedEvent(
            Guid.NewGuid(),
            "test@example.com",
            "testuser",
            "Teacher",
            DateTime.UtcNow
        );

        // Assert
        domainEvent.Should().BeAssignableTo<MediatR.INotification>();
    }

    [Test]
    public void UserActivatedEvent_ShouldImplementINotification()
    {
        // Arrange & Act
        var domainEvent = new UserActivatedEvent(
            Guid.NewGuid(),
            "test@example.com",
            DateTime.UtcNow
        );

        // Assert
        domainEvent.Should().BeAssignableTo<MediatR.INotification>();
    }

    [Test]
    public void UserProfileUpdatedEvent_ShouldImplementINotification()
    {
        // Arrange & Act
        var domainEvent = new UserProfileUpdatedEvent(
            Guid.NewGuid(),
            "TeacherProfile",
            "Updated",
            DateTime.UtcNow
        );

        // Assert
        domainEvent.Should().BeAssignableTo<MediatR.INotification>();
    }
}
