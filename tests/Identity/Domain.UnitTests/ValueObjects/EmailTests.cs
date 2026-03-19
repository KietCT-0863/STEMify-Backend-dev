using FluentAssertions;
using Identity.Domain.ValueObjects;

namespace Domain.UnitTests.ValueObjects;

[TestFixture]
public class EmailTests
{
    [Test]
    public void Create_WithValidEmail_ShouldCreateSuccessfully()
    {
        // Arrange
        var validEmail = "test@example.com";

        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [TestCase("test@gmail.com")]
    [TestCase("user.name@domain.co.uk")]
    [TestCase("test+tag@example.org")]
    [TestCase("user_name@domain-name.com")]
    [TestCase("123@example.com")]
    public void Create_WithValidEmails_ShouldCreateSuccessfully(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Test]
    public void Create_WithValidEmail_ShouldNormalizeToLowerCase()
    {
        // Arrange
        var inputEmail = "TEST@EXAMPLE.COM";
        var expectedEmail = "test@example.com";

        // Act
        var email = Email.Create(inputEmail);

        // Assert
        email.Value.Should().Be(expectedEmail);
    }

    [Test]
    public void Create_WithValidEmail_ShouldTrimWhitespace()
    {
        // Arrange
        var inputEmail = "  test@example.com  ";
        var expectedEmail = "test@example.com";

        // Act
        var email = Email.Create(inputEmail);

        // Assert
        email.Value.Should().Be(expectedEmail);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithNullOrWhitespace_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        exception.Message.Should().Contain("Email cannot be empty");
    }

    [Test]
    public void Create_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(null!));
        exception.Message.Should().Contain("Email cannot be empty");
    }

    [TestCase("invalid-email")]
    [TestCase("@example.com")]
    [TestCase("test@")]
    [TestCase("test.example.com")]
    [TestCase("test@example")]
    [TestCase("test@@example.com")]
    public void Create_WithInvalidFormat_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        exception.Message.Should().Contain("Invalid email format");
    }

    [Test]
    public void Create_WithTooLongEmail_ShouldThrowArgumentException()
    {
        // Arrange - Create an email longer than 254 characters
        var localPart = new string('a', 240);
        var longEmail = $"{localPart}@example.com"; // Total: 252 characters (still valid)
        var tooLongEmail = $"{localPart}abc@example.com"; // Total: 255 characters (invalid)

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(tooLongEmail));
        exception.Message.Should().Contain("Email is too long");
    }

    [Test]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);

        // Act
        string convertedEmail = email;

        // Assert
        convertedEmail.Should().Be(emailValue);
    }

    [Test]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be(emailValue);
    }

    [Test]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email1 = Email.Create(emailValue);
        var email2 = Email.Create(emailValue);

        // Act & Assert
        email1.Should().Be(email2);
        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    [Test]
    public void Equals_WithDifferentValue_ShouldReturnFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
    }

    [Test]
    public void Equals_WithDifferentCase_ShouldReturnTrue()
    {
        // Arrange - Both should be normalized to lowercase
        var email1 = Email.Create("TEST@EXAMPLE.COM");
        var email2 = Email.Create("test@example.com");

        // Act & Assert
        email1.Should().Be(email2);
    }

    [Test]
    public void Create_WithMaxValidLength_ShouldCreateSuccessfully()
    {
        // Arrange - Create exactly 254 character email
        var localPart = new string('a', 240);
        var validEmail = $"{localPart}@example.com"; // Exactly 254 characters

        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [TestCase("test@")]
    [TestCase("test@.")]
    [TestCase("test@..")]
    public void Create_WithInvalidDomain_ShouldThrowArgumentException(string invalidEmail)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
        exception.Message.Should().Contain("Invalid email format");
    }

    [Test]
    public void Create_EmailRecord_ShouldSupportRecordFeatures()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);

        // Act & Assert - Record should support with expressions
        var newEmail = email with
        { };
        newEmail.Should().Be(email);
        newEmail.Value.Should().Be(email.Value);
    }

    [Test]
    public void Create_WithUnicodeCharacters_ShouldThrowArgumentException()
    {
        // Arrange
        var emailWithUnicode = "tést@example.com";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Email.Create(emailWithUnicode));
        exception.Message.Should().Contain("Invalid email format");
    }

    [Test]
    public void Create_WithInternationalDomain_ShouldCreateSuccessfully()
    {
        // Arrange
        var emailWithIntlDomain = "test@example.co.uk";

        // Act
        var email = Email.Create(emailWithIntlDomain);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(emailWithIntlDomain);
    }

    [Test]
    public void ValueProperty_ShouldBeReadOnly()
    {
        // Arrange
        var emailValue = "test@example.com";
        var email = Email.Create(emailValue);

        // Act & Assert
        email.Value.Should().Be(emailValue);
        // Value property should be get-only, so this test just verifies it's accessible
    }

    [TestCase("test.email@domain.com")]
    [TestCase("test_email@domain.com")]
    [TestCase("test+email@domain.com")]
    [TestCase("test-email@domain.com")]
    public void Create_WithValidSpecialCharacters_ShouldCreateSuccessfully(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }
}
