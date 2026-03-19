using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Users.Commands.CreateUser;
using MediatR;
using Shared.Protos.User;

namespace Identity.Application.Users.Commands.EnsureUsersExist;

public class EnsureUsersExistCommandHandler : IRequestHandler<EnsureUsersExistCommand, CheckUserExistsResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IMediator _mediator;

    public EnsureUsersExistCommandHandler(IUserRepository userRepository, IMediator mediator)
    {
        _userRepository = userRepository;
        _mediator = mediator;
    }

    public async Task<CheckUserExistsResponse> Handle(EnsureUsersExistCommand request, CancellationToken cancellationToken)
    {
        var response = new CheckUserExistsResponse();
        var normalized = request.Emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var email in normalized)
        {
            var existing = await _userRepository.GetByEmailAsync(email);
            if (existing == null)
            {
                var pwd = $"{Guid.NewGuid():N}aA!1";
                var create = new CreateUserCommand
                {
                    Email = email,
                    UserName = email,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    Password = pwd,
                    Role = Identity.Domain.Enums.UserRole.Member
                };
                var created = await _mediator.Send(create, cancellationToken);
                response.Results.Add(new CheckUserExistsResult
                {
                    UserId = created.Id.ToString(),
                    Email = email
                });
            }
            else
            {
                response.Results.Add(new CheckUserExistsResult
                {
                    UserId = existing.Id.ToString(),
                    Email = existing.Email ?? string.Empty
                });
            }
        }

        return response;
    }
}


