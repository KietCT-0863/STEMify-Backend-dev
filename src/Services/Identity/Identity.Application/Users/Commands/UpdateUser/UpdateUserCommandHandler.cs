using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Identity.Application.Users.Commands.UpdateUser;

/// <summary>
/// Handler for UpdateUserCommand using TPT pattern with UserManager
/// </summary>
public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager
    )
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(request.UserId);

        var updateRequired = false;

        // Update FirstName & LastName using domain method
        if (!string.IsNullOrWhiteSpace(request.FirstName) || !string.IsNullOrWhiteSpace(request.LastName))
        {
            var firstName = !string.IsNullOrWhiteSpace(request.FirstName)
                ? request.FirstName
                : user.FirstName;
            var lastName = !string.IsNullOrWhiteSpace(request.LastName)
                ? request.LastName
                : user.LastName;

            if (firstName != user.FirstName || lastName != user.LastName)
            {
                user.UpdateName(firstName, lastName);
                updateRequired = true;
            }
        }

        // Update Role
        if (request.UserRole.HasValue && request.UserRole.Value != user.Role)
        {
            // Remove old role
            var oldRole = user.Role.ToString();
            var removeRoleResult = await _userManager.RemoveFromRoleAsync(user, oldRole);
            if (!removeRoleResult.Succeeded)
            {
                var errors = string.Join(", ", removeRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to remove old user role: {errors}");
            }

            // Update role in entity using domain method
            user.UpdateRole(request.UserRole.Value);

            // Add new role
            var newRole = request.UserRole.Value.ToString();
            var roleResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update user role: {errors}");
            }
            updateRequired = true;
        }

        // Update Status using domain method
        if (request.Status.HasValue && request.Status.Value != user.Status)
        {
            user.UpdateStatus(request.Status.Value);
            updateRequired = true;
        }

        // Update Password
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update password: {errors}");
            }
            updateRequired = true;
        }

        if (updateRequired)
        {
            // Update the user entity to trigger UpdatedAt timestamp
            user.UpdateTimestamp();

            // Update user in repository
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}