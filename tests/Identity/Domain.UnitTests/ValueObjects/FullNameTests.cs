using FluentAssertions;
using Identity.Domain.ValueObjects;

namespace Domain.UnitTests.ValueObjects;

[TestFixture]
public class FullNameTests
{
    [Test]
    public void Create_WithValidFullName_ShouldCreateSuccessfully()
    {
        // Arrange
        var validFullName = "Nguyễn Văn An";

        // Act
        var fullName = FullName.Create(validFullName);

        // Assert
        fullName.Should().NotBeNull();
        fullName.Value.Should().Be(validFullName);
    }

    [TestCase("Nguyễn Văn An")]
    [TestCase("Trần Thị Bảo")]
    [TestCase("Lê Hoàng Minh")]
    [TestCase("Phạm Quốc Anh")]
    [TestCase("Đỗ Thị Mai")]
    public void Create_WithValidVietnameseNames_ShouldCreateSuccessfully(string validName)
    {
        // Act
        var fullName = FullName.Create(validName);

        // Assert
        fullName.Should().NotBeNull();
        fullName.Value.Should().Be(validName);
    }

    [TestCase("John Smith")]
    [TestCase("Mary Jane")]
    [TestCase("Robert O'Connor")]
    [TestCase("Jean-Pierre Dubois")]
    [TestCase("Anna-Maria González")]
    public void Create_WithValidInternationalNames_ShouldCreateSuccessfully(string validName)
    {
        // Act
        var fullName = FullName.Create(validName);

        // Assert
        fullName.Should().NotBeNull();
        fullName.Value.Should().Be(validName);
    }

    [Test]
    public void Create_WithValidFullName_ShouldTrimWhitespace()
    {
        // Arrange
        var inputName = "  Nguyễn Văn An  ";
        var expectedName = "Nguyễn Văn An";

        // Act
        var fullName = FullName.Create(inputName);

        // Assert
        fullName.Value.Should().Be(expectedName);
    }

    [Test]
    public void Create_WithMultipleSpaces_ShouldNormalizeSpaces()
    {
        // Arrange
        var inputName = "Nguyễn    Văn     An";
        var expectedName = "Nguyễn Văn An";

        // Act
        var fullName = FullName.Create(inputName);

        // Assert
        fullName.Value.Should().Be(expectedName);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t")]
    [TestCase("\n")]
    public void Create_WithNullOrWhitespace_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => FullName.Create(invalidName));
        exception.Message.Should().Contain("Full name cannot be empty");
    }

    [Test]
    public void Create_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => FullName.Create(null!));
        exception.Message.Should().Contain("Full name cannot be empty");
    }

    [TestCase("A")]
    [TestCase("B")]
    public void Create_WithTooShortName_ShouldThrowArgumentException(string shortName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => FullName.Create(shortName));
        exception.Message.Should().Contain("Full name must be at least 2 characters");
    }

    [Test]
    public void Create_WithTooLongName_ShouldThrowArgumentException()
    {
        // Arrange - Create a name longer than 100 characters
        var longName = new string('A', 101);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => FullName.Create(longName));
        exception.Message.Should().Contain("Full name cannot be more than 100 characters");
    }

    [TestCase("Nguyễn123")]
    [TestCase("Test@Name")]
    [TestCase("Name#123")]
    [TestCase("User$Name")]
    [TestCase("Name%Test")]
    public void Create_WithInvalidCharacters_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => FullName.Create(invalidName));
        exception
            .Message.Should()
            .Contain("Full name can only contain letters, spaces, quotes and hyphens");
    }

    [Test]
    public void Create_WithMinValidLength_ShouldCreateSuccessfully()
    {
        // Arrange
        var minLengthName = "An"; // Exactly 2 characters

        // Act
        var fullName = FullName.Create(minLengthName);

        // Assert
        fullName.Should().NotBeNull();
        fullName.Value.Should().Be(minLengthName);
    }

    [Test]
    public void Create_WithMaxValidLength_ShouldCreateSuccessfully()
    {
        // Arrange - Create exactly 100 character name
        var maxLengthName = new string('A', 100);

        // Act
        var fullName = FullName.Create(maxLengthName);

        // Assert
        fullName.Should().NotBeNull();
        fullName.Value.Should().Be(maxLengthName);
    }

    [Test]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var nameValue = "Nguyễn Văn An";
        var fullName = FullName.Create(nameValue);

        // Act
        string convertedName = fullName;

        // Assert
        convertedName.Should().Be(nameValue);
    }

    [Test]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var nameValue = "Nguyễn Văn An";
        var fullName = FullName.Create(nameValue);

        // Act
        var result = fullName.ToString();

        // Assert
        result.Should().Be(nameValue);
    }

    [Test]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var nameValue = "Nguyễn Văn An";
        var fullName1 = FullName.Create(nameValue);
        var fullName2 = FullName.Create(nameValue);

        // Act & Assert
        fullName1.Should().Be(fullName2);
        fullName1.GetHashCode().Should().Be(fullName2.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var fullName1 = FullName.Create("Nguyễn Văn An");
        var fullName2 = FullName.Create("Trần Thị Bảo");

        // Act & Assert
        fullName1.Should().NotBe(fullName2);
    }

    [Test]
    public void Create_FullNameRecord_ShouldSupportRecordFeatures()
    {
        // Arrange
        var nameValue = "Nguyễn Văn An";
        var fullName = FullName.Create(nameValue);

        // Act & Assert - Record should support with expressions
        var newFullName = fullName with
        { };
        newFullName.Should().Be(fullName);
        newFullName.Value.Should().Be(fullName.Value);
    }

    [Test]
    public void ValueProperty_ShouldBeReadOnly()
    {
        // Arrange
        var nameValue = "Nguyễn Văn An";
        var fullName = FullName.Create(nameValue);

        // Act & Assert
        fullName.Value.Should().Be(nameValue);
        // Value property should be get-only, so this test just verifies it's accessible
    }

    [TestCase("Nguyễn Thị Mai-Hương")]
    [TestCase("O'Connor")]
    [TestCase("Jean-Pierre")]
    [TestCase("Mary O'Brien")]
    [TestCase("Anne-Marie")]
    public void Create_WithValidSpecialCharacters_ShouldCreateSuccessfully(string validName)
    {
        // Act
        var fullName = FullName.Create(validName);

        // Assert
        fullName.Should().NotBeNull();
        fullName.Value.Should().Be(validName);
    }

    [Test]
    public void Create_WithTabsAndNewlines_ShouldNormalizeToSpaces()
    {
        // Arrange
        var inputName = "Nguyễn\tVăn\nAn";
        var expectedName = "Nguyễn Văn An";

        // Act
        var fullName = FullName.Create(inputName);

        // Assert
        fullName.Value.Should().Be(expectedName);
    }
}
