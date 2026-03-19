using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Models.Auth;
using MediatR;

namespace Identity.Application.Users.Queries.GetAllUsers;

/// <summary>
/// Handler for GetAllUsersQuery
/// </summary>
public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, GetAllUsersResponse>
{
    private readonly IIdentityUnitOfWork _unitOfWork;

    public GetAllUsersQueryHandler(IIdentityUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetAllUsersResponse> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        //var spec = new UsersWithProfilesSpecification(
        //    request.IsProfileComplete,
        //    request.HasSpecialization
        //);

        //var totalCount = await _unitOfWork.Users.CountAsync(spec, cancellationToken);
        //var allUsers = await _unitOfWork.Users.GetAllAsync(spec, cancellationToken);

        var allUsers = await _unitOfWork.Users.GetAllAsync(cancellationToken);

        // Apply optional in-memory filters
        var filteredUsers = allUsers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            filteredUsers = filteredUsers.Where(user =>
                (!string.IsNullOrEmpty(user.Email) && user.Email.ToLowerInvariant().Contains(search)) ||
                (!string.IsNullOrEmpty(user.UserName) && user.UserName.ToLowerInvariant().Contains(search)) ||
                (!string.IsNullOrEmpty(user.FirstName) && user.FirstName.ToLowerInvariant().Contains(search)) ||
                (!string.IsNullOrEmpty(user.LastName) && user.LastName.ToLowerInvariant().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim().ToLowerInvariant();
            filteredUsers = filteredUsers.Where(user =>
                user.Role.ToString().ToLowerInvariant() == role);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim().ToLowerInvariant();
            filteredUsers = filteredUsers.Where(user =>
                user.Status.ToString().ToLowerInvariant() == status);
        }

        var filteredList = filteredUsers.ToList();
        var totalCount = filteredList.Count;

        // Manual pagination
        var pagedUsers = filteredList
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        var userDtos = pagedUsers
            .Select(user =>
            {
                var userInfo = new UserInfoDto
                {
                    Sub = user.Id.ToString(),
                    Email = user.Email,
                    EmailVerified = user.IsEmailConfirmed(),
                    Name = user.UserName,
                    UserType = user.Role.ToString().ToLowerInvariant(),
                    UserName = user.UserName,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Status = user.Status,
                };

                return userInfo;
            })
            .ToList();

        return new GetAllUsersResponse(userDtos, request.PageNumber, request.PageSize, totalCount);
    }
}
