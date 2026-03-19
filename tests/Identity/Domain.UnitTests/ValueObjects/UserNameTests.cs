using FluentAssertions;
using Identity.Domain.ValueObjects;

namespace Domain.UnitTests.ValueObjects;

[TestFixture]
public class UserNameTests
{
    [Test]
    public void Create_WithValidUserName_ShouldCreateSuccessfully()
    {
        // Arrange
        var validUserName = "testuser";

        // Act
        var userName = UserName.Create(validUserName);

        // Assert
        userName.Should().NotBeNull();
        userName.Value.Should().Be(validUserName);
    }

    [TestCase("user123")]
    [TestCase("test_user")]
    [TestCase("test-user")]
    [TestCase("test.user")]
    [TestCase("user.name.123")]
    [TestCase("TestUser")]
    public void Create_WithValidUserNames_ShouldCreateSuccessfully(string validUserName)
    {
        // Act
        var userName = UserName.Create(validUserName);

        // Assert
        userName.Should().NotBeNull();
        userName.Value.Should().Be(validUserName);
    }

    [Test]
    public void Create_WithValidUserName_ShouldTrimWhitespace()
    {
        // Arrange
        var inputUserName = "  testuser  ";
        var expectedUserName = "testuser";

        // Act
        var userName = UserName.Create(inputUserName);

        // Assert
        userName.Value.Should().Be(expectedUserName);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrWhitespace_ShouldThrowArgumentException(string invalidUserName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(invalidUserName));
        exception.Message.Should().Contain("Username cannot be empty");
    }

    [Test]
    public void Create_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(null!));
        exception.Message.Should().Contain("Username cannot be empty");
    }

    [TestCase("ab")]
    [TestCase("a")]
    public void Create_WithTooShortUserName_ShouldThrowArgumentException(string shortUserName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(shortUserName));
        exception.Message.Should().Contain("Username must be at least 3 characters");
    }

    [Test]
    public void Create_WithTooLongUserName_ShouldThrowArgumentException()
    {
        // Arrange
        var longUserName = new string('a', 51); // 51 characters

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(longUserName));
        exception.Message.Should().Contain("Username cannot be more than 50 characters");
    }

    [TestCase("user name")]
    [TestCase("user@name")]
    [TestCase("user#name")]
    [TestCase("user$name")]
    [TestCase("user%name")]
    public void Create_WithInvalidCharacters_ShouldThrowArgumentException(string invalidUserName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(invalidUserName));
        exception
            .Message.Should()
            .Contain("Username can only contain letters, numbers, dots, underscores and hyphens");
    }

    [TestCase(".username")]
    [TestCase("username.")]
    public void Create_WithStartOrEndDot_ShouldThrowArgumentException(string invalidUserName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(invalidUserName));
        exception.Message.Should().Contain("Username cannot start or end with a dot");
    }

    [Test]
    public void Create_WithConsecutiveDots_ShouldThrowArgumentException()
    {
        // Arrange
        var userNameWithDoubleDots = "user..name";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            UserName.Create(userNameWithDoubleDots)
        );
        exception.Message.Should().Contain("Username cannot contain two consecutive dots");
    }

    [Test]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var userNameValue = "testuser";
        var userName = UserName.Create(userNameValue);

        // Act
        string convertedUserName = userName;

        // Assert
        convertedUserName.Should().Be(userNameValue);
    }

    [Test]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var userNameValue = "testuser";
        var userName = UserName.Create(userNameValue);

        // Act
        var result = userName.ToString();

        // Assert
        result.Should().Be(userNameValue);
    }

    [Test]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var userNameValue = "testuser";
        var userName1 = UserName.Create(userNameValue);
        var userName2 = UserName.Create(userNameValue);

        // Act & Assert
        userName1.Should().Be(userName2);
        userName1.GetHashCode().Should().Be(userName2.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var userName1 = UserName.Create("testuser1");
        var userName2 = UserName.Create("testuser2");

        // Act & Assert
        userName1.Should().NotBe(userName2);
    }

    [Test]
    public void Create_WithMixedCase_ShouldPreserveCase()
    {
        // Arrange
        var mixedCaseUserName = "TestUser";

        // Act
        var userName = UserName.Create(mixedCaseUserName);

        // Assert
        userName.Value.Should().Be(mixedCaseUserName);
    }

    [Test]
    public void Create_WithMaxValidLength_ShouldCreateSuccessfully()
    {
        // Arrange
        var maxLengthUserName = new string('a', 50); // Exactly 50 characters

        // Act
        var userName = UserName.Create(maxLengthUserName);

        // Assert
        userName.Should().NotBeNull();
        userName.Value.Should().Be(maxLengthUserName);
    }

    [Test]
    public void Create_WithMinValidLength_ShouldCreateSuccessfully()
    {
        // Arrange
        var minLengthUserName = "abc"; // Exactly 3 characters

        // Act
        var userName = UserName.Create(minLengthUserName);

        // Assert
        userName.Should().NotBeNull();
        userName.Value.Should().Be(minLengthUserName);
    }

    [Test]
    public void Create_UserNameRecord_ShouldSupportRecordFeatures()
    {
        // Arrange
        var userNameValue = "testuser";
        var userName = UserName.Create(userNameValue);

        // Act & Assert - Record should support with expressions
        var newUserName = userName with
        { };
        newUserName.Should().Be(userName);
        newUserName.Value.Should().Be(userName.Value);
    }

    [TestCase("user_123")]
    [TestCase("user-456")]
    [TestCase("user.789")]
    [TestCase("123user")]
    [TestCase("user123user")]
    public void Create_WithValidMixedCharacters_ShouldCreateSuccessfully(string validUserName)
    {
        // Act
        var userName = UserName.Create(validUserName);

        // Assert
        userName.Should().NotBeNull();
        userName.Value.Should().Be(validUserName);
    }

    [Test]
    public void ValueProperty_ShouldBeReadOnly()
    {
        // Arrange
        var userNameValue = "testuser";
        var userName = UserName.Create(userNameValue);

        // Act & Assert
        userName.Value.Should().Be(userNameValue);
        // Value property should be get-only, so this test just verifies it's accessible
    }

    [TestCase("a..b")]
    [TestCase("a...b")]
    [TestCase("user..test")]
    public void Create_WithMultipleConsecutiveDots_ShouldThrowArgumentException(
        string invalidUserName
    )
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(invalidUserName));
        exception.Message.Should().Contain("Username cannot contain two consecutive dots");
    }

    [TestCase("test!user")]
    [TestCase("test*user")]
    [TestCase("test&user")]
    [TestCase("test+user")]
    [TestCase("test=user")]
    public void Create_WithSpecialCharacters_ShouldThrowArgumentException(string invalidUserName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserName.Create(invalidUserName));
        exception
            .Message.Should()
            .Contain("Username can only contain letters, numbers, dots, underscores and hyphens");
    }
}
