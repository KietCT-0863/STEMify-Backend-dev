using FluentAssertions;
using Identity.Domain.Entities;

namespace Domain.UnitTests.Entities;

[TestFixture]
public class TeacherProfileTests
{
    private static readonly string TestFirstName = "Dr. Nguyễn Văn";
    private static readonly string TestLastName = "Giảng";

    [Test]
    public void Create_WithValidData_ShouldCreateTeacherProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bio = "Experienced educator with 10+ years in STEM";
        var specialization = "Computer Science & AI";

        // Act
        var profile = TeacherProfile.Create(
            userId,
            TestFirstName,
            TestLastName,
            bio,
            specialization
        );

        // Assert
        profile.Id.Should().Be(userId);
        profile.FirstName.Should().Be(TestFirstName);
        profile.LastName.Should().Be(TestLastName);
        profile.FullName.Should().Be($"{TestFirstName} {TestLastName}");
        profile.Bio.Should().Be(bio);
        profile.Specialization.Should().Be(specialization);
        profile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        profile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Create_WithOptionalParametersNull_ShouldCreateTeacherProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var profile = TeacherProfile.Create(userId, TestFirstName, TestLastName);

        // Assert
        profile.Bio.Should().BeNull();
        profile.Specialization.Should().BeNull();
    }

    [Test]
    public void Create_WithEmptyStrings_ShouldCreateWithEmptyValues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bio = "";
        var specialization = "";

        // Act
        var profile = TeacherProfile.Create(
            userId,
            TestFirstName,
            TestLastName,
            bio,
            specialization
        );

        // Assert
        profile.Bio.Should().Be(bio);
        profile.Specialization.Should().Be(specialization);
    }

    #region Update Profile Tests

    [Test]
    public void UpdateProfile_WithValidData_ShouldUpdateSuccessfully()
    {
        // Arrange
        var profile = TeacherProfile.Create(Guid.NewGuid(), TestFirstName, TestLastName);
        var newFirstName = "Prof. Trần Thị";
        var newLastName = "Updated";
        var newBio = "Updated bio with new achievements";
        var newSpecialization = "Machine Learning & Data Science";
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.UpdateProfile(newFirstName, newLastName, newBio, newSpecialization);

        // Assert
        profile.FirstName.Should().Be(newFirstName);
        profile.LastName.Should().Be(newLastName);
        profile.FullName.Should().Be($"{newFirstName} {newLastName}");
        profile.Bio.Should().Be(newBio);
        profile.Specialization.Should().Be(newSpecialization);
        profile.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Test]
    public void UpdateProfile_WithSameValues_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var bio = "Original bio";
        var specialization = "Original specialization";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            bio,
            specialization
        );
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.UpdateProfile(TestFirstName, TestLastName, bio, specialization);

        // Assert
        profile.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Test]
    public void UpdateProfile_WithOnlyOneField_ShouldUpdateOnlyThatField()
    {
        // Arrange
        var originalBio = "Original bio";
        var originalSpecialization = "Original specialization";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            originalBio,
            originalSpecialization
        );
        var newBio = "Updated bio";

        // Act
        profile.UpdateProfile(bio: newBio);

        // Assert
        profile.Bio.Should().Be(newBio);
        profile.Specialization.Should().Be(originalSpecialization); // Should remain unchanged
        profile.FirstName.Should().Be(TestFirstName); // Should remain unchanged
        profile.LastName.Should().Be(TestLastName); // Should remain unchanged
    }

    [Test]
    public void UpdateProfile_WithNullValues_ShouldClearFields()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Specialization"
        );

        // Act
        profile.UpdateProfile(bio: null, specialization: null);

        // Assert
        profile.Bio.Should().BeNull();
        profile.Specialization.Should().BeNull();
    }

    [Test]
    public void UpdateProfile_WithEmptyStrings_ShouldSetEmptyValues()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Specialization"
        );

        // Act
        profile.UpdateProfile(bio: "", specialization: "");

        // Assert
        profile.Bio.Should().Be("");
        profile.Specialization.Should().Be("");
    }

    [Test]
    public void UpdateProfile_WithPartialUpdate_ShouldUpdateOnlySpecifiedFields()
    {
        // Arrange
        var originalBio = "Original bio";
        var originalSpecialization = "Original specialization";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            originalBio,
            originalSpecialization
        );
        var newSpecialization = "Updated specialization";

        // Act
        profile.UpdateProfile(specialization: newSpecialization);

        // Assert
        profile.Bio.Should().Be(originalBio); // Should remain unchanged
        profile.Specialization.Should().Be(newSpecialization);
        profile.FirstName.Should().Be(TestFirstName); // Should remain unchanged
        profile.LastName.Should().Be(TestLastName); // Should remain unchanged
    }

    #endregion

    #region UpdateSpecialization Tests

    [Test]
    public void UpdateSpecialization_WithNewSpecialization_ShouldUpdateSuccessfully()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Old Specialization"
        );
        var newSpecialization = "New Specialization";
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.UpdateSpecialization(newSpecialization);

        // Assert
        profile.Specialization.Should().Be(newSpecialization);
        profile.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Test]
    public void UpdateSpecialization_WithSameSpecialization_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var specialization = "Same Specialization";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            specialization
        );
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.UpdateSpecialization(specialization);

        // Assert
        profile.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Test]
    public void UpdateSpecialization_WithNullValue_ShouldSetNull()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Specialization"
        );

        // Act
        profile.UpdateSpecialization(null);

        // Assert
        profile.Specialization.Should().BeNull();
    }

    [Test]
    public void UpdateSpecialization_WithEmptyString_ShouldSetEmpty()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Specialization"
        );

        // Act
        profile.UpdateSpecialization("");

        // Assert
        profile.Specialization.Should().Be("");
    }

    #endregion

    #region Business Logic Tests

    [Test]
    public void HasSpecialization_WithSpecialization_ShouldReturnTrue()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Computer Science"
        );

        // Act & Assert
        profile.HasSpecialization().Should().BeTrue();
    }

    [Test]
    public void HasSpecialization_WithoutSpecialization_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(Guid.NewGuid(), TestFirstName, TestLastName);

        // Act & Assert
        profile.HasSpecialization().Should().BeFalse();
    }

    [Test]
    public void HasSpecialization_WithEmptySpecialization_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(Guid.NewGuid(), TestFirstName, TestLastName, "Bio", "");

        // Act & Assert
        profile.HasSpecialization().Should().BeFalse();
    }

    [Test]
    public void HasSpecialization_WithWhitespaceSpecialization_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "   "
        );

        // Act & Assert
        profile.HasSpecialization().Should().BeFalse();
    }

    [Test]
    public void IsProfileComplete_WithBothBioAndSpecialization_ShouldReturnTrue()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Specialization"
        );

        // Act & Assert
        profile.IsProfileComplete().Should().BeTrue();
    }

    [Test]
    public void IsProfileComplete_WithoutBio_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            null,
            "Specialization"
        );

        // Act & Assert
        profile.IsProfileComplete().Should().BeFalse();
    }

    [Test]
    public void IsProfileComplete_WithoutSpecialization_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            null
        );

        // Act & Assert
        profile.IsProfileComplete().Should().BeFalse();
    }

    [Test]
    public void IsProfileComplete_WithEmptyBio_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "",
            "Specialization"
        );

        // Act & Assert
        profile.IsProfileComplete().Should().BeFalse();
    }

    [Test]
    public void IsProfileComplete_WithEmptySpecialization_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(Guid.NewGuid(), TestFirstName, TestLastName, "Bio", "");

        // Act & Assert
        profile.IsProfileComplete().Should().BeFalse();
    }

    [Test]
    public void IsProfileComplete_WithWhitespaceBio_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "   ",
            "Specialization"
        );

        // Act & Assert
        profile.IsProfileComplete().Should().BeFalse();
    }

    [Test]
    public void IsProfileComplete_WithWhitespaceSpecialization_ShouldReturnFalse()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "   "
        );

        // Act & Assert
        profile.IsProfileComplete().Should().BeFalse();
    }

    #endregion

    #region Edge Cases and Domain Invariants

    [Test]
    public void TeacherProfile_ShouldMaintainCreatedAtImmutable()
    {
        // Arrange
        var profile = TeacherProfile.Create(Guid.NewGuid(), TestFirstName, TestLastName);
        var originalCreatedAt = profile.CreatedAt;

        // Act
        profile.UpdateProfile("New", "Name", "New Bio", "New Specialization");

        // Assert
        profile.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Test]
    public void TeacherProfile_UpdatedAt_ShouldChangeOnlyWhenFieldsChange()
    {
        // Arrange
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            "Specialization"
        );
        var originalUpdatedAt = profile.UpdatedAt;

        // Act - Update with same values
        profile.UpdateProfile(TestFirstName, TestLastName, "Bio", "Specialization");

        // Assert
        profile.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    [Test]
    public void TeacherProfile_WithComplexSpecializations_ShouldHandleCorrectly()
    {
        // Arrange
        var complexSpecialization =
            "Machine Learning, Artificial Intelligence, Data Science & Computer Vision";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            "Bio",
            complexSpecialization
        );

        // Act & Assert
        profile.Specialization.Should().Be(complexSpecialization);
        profile.HasSpecialization().Should().BeTrue();
    }

    [Test]
    public void TeacherProfile_WithLongBio_ShouldHandleCorrectly()
    {
        // Arrange
        var longBio = new string('A', 1000); // 1000 character bio
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            longBio,
            "Specialization"
        );

        // Act & Assert
        profile.Bio.Should().Be(longBio);
        profile.IsProfileComplete().Should().BeTrue();
    }

    [Test]
    public void TeacherProfile_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var bioWithSpecialChars = "Bio with special chars: @#$%^&*()!";
        var specializationWithSpecialChars = "C# & .NET Development";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            bioWithSpecialChars,
            specializationWithSpecialChars
        );

        // Act & Assert
        profile.Bio.Should().Be(bioWithSpecialChars);
        profile.Specialization.Should().Be(specializationWithSpecialChars);
    }

    [Test]
    public void TeacherProfile_WithVietnameseContent_ShouldHandleCorrectly()
    {
        // Arrange
        var vietnameseBio = "Giảng viên chuyên về Khoa học máy tính và Trí tuệ nhân tạo";
        var vietnameseSpecialization = "Khoa học dữ liệu và Học máy";
        var profile = TeacherProfile.Create(
            Guid.NewGuid(),
            TestFirstName,
            TestLastName,
            vietnameseBio,
            vietnameseSpecialization
        );

        // Act & Assert
        profile.Bio.Should().Be(vietnameseBio);
        profile.Specialization.Should().Be(vietnameseSpecialization);
    }

    [Test]
    public void UpdateProfile_WithMultipleConsecutiveUpdates_ShouldMaintainConsistency()
    {
        // Arrange
        var profile = TeacherProfile.Create(Guid.NewGuid(), TestFirstName, TestLastName);

        // Act
        profile.UpdateProfile(bio: "Bio 1");
        profile.UpdateProfile(specialization: "Spec 1");
        profile.UpdateProfile("New", "Name", "Bio 2", "Spec 2");

        // Assert
        profile.FirstName.Should().Be("New");
        profile.LastName.Should().Be("Name");
        profile.Bio.Should().Be("Bio 2");
        profile.Specialization.Should().Be("Spec 2");
    }

    #endregion
}
