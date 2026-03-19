using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using MediatR;

namespace Identity.Application.Users.Commands.DeleteUser;

/// <summary>
/// Handler for DeleteUserCommand - Updated for TPT pattern
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IUserRepository userRepository, IIdentityUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Find user by ID using new Application repository interface
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            throw new UserNotFoundException(request.UserId);

        // Soft delete: Change status to Disabled using base class method
        user.DeactivateUser();

        // Update user in repository
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
