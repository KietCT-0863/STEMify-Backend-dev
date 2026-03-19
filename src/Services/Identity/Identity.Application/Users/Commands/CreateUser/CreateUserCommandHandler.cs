using Common.Logging.Metrics;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager
    )
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken
    )
    {
        // Check if email already exists
        if (!await _userRepository.IsEmailUniqueAsync(request.Email))
        {
            throw new InvalidOperationException($"Email {request.Email} is already in use");
        }

        // Check if username already exists
        if (!await _userRepository.IsUserNameUniqueAsync(request.UserName))
        {
            throw new InvalidOperationException($"Username {request.UserName} is already in use");
        }

        var platformRole = NormalizePlatformRole(request.Role);

        var user = User.Create(
            Guid.NewGuid(),
            request.Email,
            request.UserName,
            request.FirstName,
            request.LastName,
            platformRole);

        // Create user with password using UserManager for proper Identity integration
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Add user to the appropriate role
        var roleResult = await _userManager.AddToRoleAsync(user, platformRole.ToString());
        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign user role: {errors}");
        }

        // Save changes to persist domain events
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Record user registration metrics
        IdentityMetrics.RecordUserRegistration(user.Role.ToString());

        return new CreateUserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
        };
    }
    private static UserRole NormalizePlatformRole(UserRole requestedRole)
    {
        return requestedRole switch
        {
            UserRole.Admin => UserRole.Admin,
            UserRole.Staff => UserRole.Staff,
            UserRole.Member => UserRole.Member,
            _ => UserRole.Member,
        };
    }
}
