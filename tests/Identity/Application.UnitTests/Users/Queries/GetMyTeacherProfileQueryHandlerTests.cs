using System.Security.Claims;
using Identity.Application.Common.Interfaces;
using Identity.Application.Users.Queries.GetTeacherProfile;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Identity.Application.UnitTests.Users.Queries;

public class GetMyTeacherProfileQueryHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Mock<ClaimsPrincipal> _userMock;
    private readonly GetMyTeacherProfileQueryHandler _handler;

    public GetMyTeacherProfileQueryHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _httpContextMock = new Mock<HttpContext>();
        _userMock = new Mock<ClaimsPrincipal>();

        _handler = new GetMyTeacherProfileQueryHandler(
            _mediatorMock.Object,
            _httpContextAccessorMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnTeacherProfile_WhenUserIsAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedResponse = new GetTeacherProfileResponse
        {
            UserId = userId,
            Email = "teacher@test.com",
            UserName = "teacher1",
            FirstName = "John",
            LastName = "Doe",
            FullName = "John Doe",
            Specialization = "Mathematics",
            IsProfileComplete = true,
            HasSpecialization = true,
        };

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };

        _userMock.Setup(x => x.FindFirst(ClaimTypes.NameIdentifier)).Returns(claims.First());

        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetTeacherProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var query = new GetMyTeacherProfileQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("teacher@test.com", result.Email);
        Assert.Equal("Mathematics", result.Specialization);

        _mediatorMock.Verify(
            x =>
                x.Send(
                    It.Is<GetTeacherProfileQuery>(q => q.UserId == userId),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserNotAuthenticated()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var query = new GetMyTeacherProfileQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(query, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIdClaimMissing()
    {
        // Arrange
        _userMock.Setup(x => x.FindFirst(ClaimTypes.NameIdentifier)).Returns((Claim?)null);

        _httpContextMock.Setup(x => x.User).Returns(_userMock.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(_httpContextMock.Object);

        var query = new GetMyTeacherProfileQuery();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(query, CancellationToken.None)
        );
    }
}
