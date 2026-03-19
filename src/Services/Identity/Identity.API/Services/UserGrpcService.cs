using Grpc.Core;
using Identity.Application.Common.Exceptions;
using Identity.Application.Common.Models.Auth;
using Identity.Application.Users.Commands.CheckUserExists;
using Identity.Application.Users.Commands.CreateUser;
using Identity.Application.Users.Commands.DeleteUser;
using Identity.Application.Users.Commands.EnsureUsersExist;
using Identity.Application.Users.Commands.UpdateUser;
using Identity.Application.Users.Queries.GetAllUsers;
using Identity.Application.Users.Queries.GetOrganizationAdmins;
using Identity.Application.Users.Queries.GetOrganizationUserById;
using Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;
using Identity.Application.Users.Queries.GetOrganizationUsersByUserId;
using Identity.Application.Users.Queries.GetUserInfo;
using Identity.Application.Users.Queries.SearchUsers;
using MediatR;
using ServiceStack;
using Shared.Protos.User;

namespace Identity.API.Services
{
    public class UserGrpcService : GrpcUser.GrpcUserBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UserGrpcService> _logger;

        public UserGrpcService(IMediator mediator, ILogger<UserGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<GrpcUserResponse?> GetUserById(
            GetUserRequest request,
            ServerCallContext context
        )
        {
            var userId = Guid.Parse(request.Id);
            var query = new GetUserInfoQuery(userId);

            UserInfoDto result;
            try
            {
                result = await _mediator.Send(query, context.CancellationToken);
            }
            catch (NotFoundException nfEx)
            {
                _logger.LogWarning(nfEx, "GetUserById: user not found for id {UserId}", request.Id);
                return null;
            }

            // Map result domain → gRPC
            var response = new GrpcUserResponse
            {
                UserId = result.Sub,
                Email = result.Email,
                ImageUrl = result.Picture,
                FirstName = result.GivenName,
                LastName = result.FamilyName,
                UserRole = result.UserType,
                Status = result.Status.ToString(),
                UserName = result.UserName
            };

            if (result.OrganizationId.HasValue)
            {
                response.OrganizationId = result.OrganizationId.Value;
            }

            return response;
        }

        public override async Task<OrganizationUserInfo> GetOrganizationUserById(
            GetOrganizationUserByIdRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.OrganizationUserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationUserId must be provided."));
            }

            if (!Guid.TryParse(request.OrganizationUserId, out var organizationUserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationUserId must be a valid GUID."));
            }

            var query = new GetOrganizationUserByIdQuery(organizationUserId);
            var result = await _mediator.Send(query, context.CancellationToken);

            if (result == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Organization user not found."));
            }

            // Chọn subscription chính để lấy thông tin OrganizationUser (ưu tiên active)
            var primarySub = result.Subscriptions
                .OrderByDescending(s => s.IsActive)
                .ThenByDescending(s => s.JoinedAt)
                .FirstOrDefault();

            var response = new OrganizationUserInfo
            {
                UserId = result.UserId.ToString(),
                Email = result.Email,
                UserName = result.UserName,
                FullName = result.FullName,
                FirstName = result.FirstName,
                LastName = result.LastName,
                LastLoginAt = result.LastLoginAt.HasValue
                    ? result.LastLoginAt.Value.ToString("O")
                    : string.Empty,

                OrganizationUserId = primarySub?.OrganizationUserId.ToString() ?? string.Empty,
                OrganizationId = primarySub?.OrganizationId ?? 0,
                OrganizationRole = primarySub?.OrganizationRole ?? string.Empty,
                LicenseType = primarySub?.LicenseType ?? string.Empty,
                LicenseAssignmentId = primarySub?.LicenseAssignmentId ?? string.Empty,
                IsActive = primarySub?.IsActive ?? false,
                JoinedAt = primarySub?.JoinedAt.ToString("O") ?? string.Empty,
                GroupName = primarySub?.GroupName ?? string.Empty,
                GroupCode = primarySub?.GroupCode ?? string.Empty,
                Bio = primarySub?.Bio ?? string.Empty,
                StudentDateOfBirth = primarySub?.StudentDateOfBirth.HasValue == true
                    ? primarySub.StudentDateOfBirth.Value.ToString("O")
                    : string.Empty,
                StudentMajor = primarySub?.StudentMajor ?? string.Empty,
                TeacherSpecialization = primarySub?.TeacherSpecialization ?? string.Empty
            };

            // Map danh sách subscription (chỉ thông tin riêng từng subscription)
            response.Subscriptions.AddRange(
                result.Subscriptions.Select(sub => new Shared.Protos.User.SubscriptionInfo
                {
                    SubscriptionOrderId = sub.SubscriptionOrderId ?? 0,
                    LicenseType = sub.LicenseType,
                    LicenseAssignmentId = sub.LicenseAssignmentId ?? string.Empty,
                    IsActive = sub.IsActive,
                    JoinedAt = sub.JoinedAt.ToString("O")
                }));

            return response;
        }


        public override async Task<GrpcUserResponse> GetUserByOrganizationUserId(
            GetOrganizationUserByIdRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.OrganizationUserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationUserId must be provided."));
            }

            if (!Guid.TryParse(request.OrganizationUserId, out var organizationUserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationUserId must be a valid GUID."));
            }

            var orgUserQuery = new GetOrganizationUserByIdQuery(organizationUserId);
            var orgUser = await _mediator.Send(orgUserQuery, context.CancellationToken);

            if (orgUser == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Organization user not found."));
            }


            var userInfoQuery = new GetUserInfoQuery(orgUser.UserId);

            UserInfoDto userInfo;
            try
            {
                userInfo = await _mediator.Send(userInfoQuery, context.CancellationToken);
            }
            catch (NotFoundException nfEx)
            {
                _logger.LogWarning(
                    nfEx,
                    "GetUserByOrganizationUserId: user not found for OrganizationUserId {OrganizationUserId}, UserId {UserId}",
                    request.OrganizationUserId,
                    orgUser.UserId);

                throw new RpcException(new Status(StatusCode.NotFound, "User not found for this organization user."));
            }

            var response = new GrpcUserResponse
            {
                UserId = userInfo.Sub,
                Email = userInfo.Email,
                ImageUrl = userInfo.Picture,
                FirstName = userInfo.GivenName,
                LastName = userInfo.FamilyName,
                UserRole = userInfo.UserType,
                Status = userInfo.Status.ToString(),
                UserName = userInfo.UserName
            };

            if (userInfo.OrganizationId.HasValue)
            {
                response.OrganizationId = userInfo.OrganizationId.Value;
            }

            return response;
        }

        public override async Task<GetOrganizationAdminsResponse> GetOrganizationAdmins(
            GetOrganizationAdminsRequest request,
            ServerCallContext context)
        {
            if (request.OrganizationId <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationId must be greater than zero."));
            }

            var query = new GetOrganizationAdminsQuery(request.OrganizationId);
            var admins = await _mediator.Send(query, context.CancellationToken);

            var response = new GetOrganizationAdminsResponse();
            response.Admins.AddRange(
                admins.Select(admin => new OrganizationAdminSummary
                {
                    UserId = admin.UserId,
                    Email = admin.Email,
                    FullName = admin.FullName
                }));

            return response;
        }

        public override async Task<OrganizationUserModel> GetOrganizationUser(
            GetOrganizationUserRequest request,
            ServerCallContext context)
        {
            if (request.OrganizationId <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationId must be greater than zero."));
            }

            var query = new GetOrganizationUserQuery(Guid.Parse(request.UserId), request.OrganizationId);
            return await _mediator.Send(query, context.CancellationToken);
        }

        public override async Task<PagedOrganizationUserList> GetOrganizationUsers(
            GetOrganizationUsersRequest request,
            ServerCallContext context)
        {
            if (request.OrganizationId <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationId must be greater than zero."));
            }

            var query = new GetOrganizationUsersByOrganizationIdQuery
            {
                OrganizationId = request.OrganizationId,
                ActiveOnly = request.ActiveOnly,
                PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 100,
                Role = request.Role,
                SubscriptionOrderId = request.SubscriptionOrderId,
                Status = request.Status,
                Search = request.Search,
                GroupId = request.GroupId >= 0 ? request.GroupId : null
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new PagedOrganizationUserList
            {
                PageNumber = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                Items =
            {
                result.Items.Select(user =>
                {
                    var primarySub = user.Subscriptions
                        .OrderByDescending(s => s.IsActive)
                        .ThenByDescending(s => s.JoinedAt)
                        .FirstOrDefault();

                    return new SingleOrganizationUserInfo
                {
                    UserId = user.UserId.ToString(),
                    Email = user.Email,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    FirstName = user.FirstName,
                        LastName = user.LastName,
                        LastLoginAt = user.LastLoginAt.HasValue
                            ? user.LastLoginAt.Value.ToString("O")
                            : string.Empty,

                        OrganizationUserId = primarySub?.OrganizationUserId.ToString() ?? string.Empty,
                        OrganizationId = primarySub?.OrganizationId ?? 0,
                        //OrganizationRole = primarySub?.OrganizationRole ?? string.Empty,
                        //LicenseType = primarySub?.LicenseType ?? string.Empty,
                        LicenseAssignmentId = primarySub?.LicenseAssignmentId ?? string.Empty,
                        IsActive = primarySub?.IsActive ?? false,
                        JoinedAt = primarySub?.JoinedAt.ToString("O") ?? string.Empty,
                        GroupName = primarySub?.GroupName ?? string.Empty,
                        GroupCode = primarySub?.GroupCode ?? string.Empty,
                        Bio = primarySub?.Bio ?? string.Empty,
                        StudentDateOfBirth = primarySub?.StudentDateOfBirth.HasValue == true
                            ? primarySub.StudentDateOfBirth.Value.ToString("O")
                            : string.Empty,
                        StudentMajor = primarySub?.StudentMajor ?? string.Empty,
                        TeacherSpecialization = primarySub?.TeacherSpecialization ?? string.Empty,

                        Subscriptions =
                    {
                        user.Subscriptions.Select(sub => new Shared.Protos.User.SubscriptionInfo
                        {
                                SubscriptionOrderId = sub.SubscriptionOrderId ?? 0,
                                LicenseType = sub.LicenseType,
                                LicenseAssignmentId = sub.LicenseAssignmentId ?? string.Empty,
                                IsActive = sub.IsActive,
                                JoinedAt = sub.JoinedAt.ToString("O")
                        })
                    }
                    };
                })
            }
            };

            return response;
        }

        public override async Task<PagedOrganizationUserList> GetOrganizationUsersByUserId(
            GetOrganizationUsersByUserIdRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId must be provided."));
            }

            if (!Guid.TryParse(request.UserId, out var userId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId must be a valid GUID."));
            }

            var query = new GetOrganizationUsersByUserIdQuery(userId)
            {
                ActiveOnly = false,
                PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 100
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var flattenedMemberships = result.Items
                .SelectMany(user => user.Subscriptions.Select(sub => new { user, sub }))
                .ToList();

            var response = new PagedOrganizationUserList
            {
                PageNumber = result.Page,
                PageSize = result.PageSize,
                TotalCount = flattenedMemberships.Count,
                TotalPages = result.TotalPages,
            };

            response.Items.AddRange(
                flattenedMemberships.Select(ms => new SingleOrganizationUserInfo
                {
                    UserId = ms.user.UserId.ToString(),
                    Email = ms.user.Email,
                    UserName = ms.user.UserName,
                    FullName = ms.user.FullName,
                    FirstName = ms.user.FirstName,
                    LastName = ms.user.LastName,
                    LastLoginAt = ms.user.LastLoginAt.HasValue
                        ? ms.user.LastLoginAt.Value.ToString("O")
                        : string.Empty,

                    OrganizationUserId = ms.sub.OrganizationUserId.ToString(),
                    OrganizationId = ms.sub.OrganizationId,
                    //OrganizationRole = ms.sub.OrganizationRole,
                    //LicenseType = ms.sub.LicenseType,
                    LicenseAssignmentId = ms.sub.LicenseAssignmentId ?? string.Empty,
                    IsActive = ms.sub.IsActive,
                    JoinedAt = ms.sub.JoinedAt.ToString("O"),
                    GroupName = ms.sub.GroupName ?? string.Empty,
                    GroupCode = ms.sub.GroupCode ?? string.Empty,
                    Bio = ms.sub.Bio ?? string.Empty,
                    StudentDateOfBirth = ms.sub.StudentDateOfBirth.HasValue
                        ? ms.sub.StudentDateOfBirth.Value.ToString("O")
                        : string.Empty,
                    StudentMajor = ms.sub.StudentMajor ?? string.Empty,
                    TeacherSpecialization = ms.sub.TeacherSpecialization ?? string.Empty,

                    Subscriptions =
                    {
                    new Shared.Protos.User.SubscriptionInfo
                    {
                        SubscriptionOrderId = ms.sub.SubscriptionOrderId ?? 0,
                        LicenseType = ms.sub.LicenseType,
                        LicenseAssignmentId = ms.sub.LicenseAssignmentId ?? string.Empty,
                        IsActive = ms.sub.IsActive,
                        JoinedAt = ms.sub.JoinedAt.ToString("O")
                    }
                    }
                }));

            return response;
        }

        public override async Task<PagedUserList> QueryUsers(
            QueryUsersRequest request,
            ServerCallContext context
        )
        {
            var query = new GetAllUsersQuery()
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search,
                Role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role,
                Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
            };
            var result = await _mediator.Send(query);

            var response = new PagedUserList
            {
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                Items =
                {
                    result.Data.Select(user => new GrpcUserResponse
                    {
                        UserId = user.Sub,
                        FirstName = user.FirstName ?? "",
                        LastName = user.LastName ?? "",
                        UserName = user.UserName ?? "",
                        Email = user.Email,
                        ImageUrl = user.Picture,
                        UserRole = user.UserType,
                        Status = user.Status.ToString()
                            }),
                },
            };

            return response;
        }


        public override async Task<PagedUserList> SearchUsers(
            SearchUsersRequest request,
            ServerCallContext context)
        {
            var unifiedQuery = new SearchUsersQuery
            {
                OrganizationId = 0,
                SubscriptionOrderId = request.SubscriptionOrderId > 0 ? request.SubscriptionOrderId : null,
                LicenseType = string.IsNullOrWhiteSpace(request.LicenseType) ? null : request.LicenseType,
                Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search,
                OrderBy = string.IsNullOrWhiteSpace(request.OrderBy) ? null : request.OrderBy,
                Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
                Role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await _mediator.Send(unifiedQuery, context.CancellationToken);

            var response = new PagedUserList
            {
                PageNumber = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                Items =
                {
                    result.Items.Select(user => new GrpcUserResponse
                    {
                        UserId = user.Id.ToString(),
                        FirstName = user.FirstName ?? string.Empty,
                        LastName = user.LastName ?? string.Empty,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email,
                        UserRole = user.UserType,
                        Status = user.Status.ToString()
                    })
                }
            };

            return response;
        }

        private static UserStatus MapStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status)) return UserStatus.Pending;
            switch (status.Trim().ToUpperInvariant())
            {
                case "PENDING": return UserStatus.Pending;
                case "ACTIVE": return UserStatus.Active;
                case "DISABLED": return UserStatus.Disabled;
                case "DELETED": return UserStatus.Deleted;
                case "LOCKED": return UserStatus.Locked;
                default: return UserStatus.Pending;
            }
        }

        public override async Task<Shared.Protos.User.CreateUserResponse> CreateUser(
            CreateUserRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateUserCommand()
            {
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = request.UserRole.ToEnumOrDefault(Domain.Enums.UserRole.Member),
            };
            var result = await _mediator.Send(command);

            // Map result domain → gRPC
            var response = new Shared.Protos.User.CreateUserResponse { UserId = result.Id.ToString() };

            return response;
        }

        public override async Task<UpdateUserResponse> UpdateUser(
            UpdateUserRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateUserCommand()
            {
                UserId = Guid.Parse(request.Id),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.UserStatus.Active),
                Password = request.Password,
                UserRole = string.IsNullOrEmpty(request.UserRole) ? null : request.UserRole.ToEnumOrDefault(Domain.Enums.UserRole.Member)
            };
            var result = await _mediator.Send(command);

            // Map result domain → gRPC
            var response = new UpdateUserResponse { IsSuccess = result };

            return response;
        }

        public override async Task<DeleteUserResponse> DeleteUser(
            DeleteUserRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteUserCommand(Guid.Parse(request.Id));
            var result = await _mediator.Send(command);

            // Map result domain → gRPC
            var response = new DeleteUserResponse { IsSuccess = result };

            return response;
        }

        public override async Task<CheckUserExistsResponse> CheckUserExists(
            CheckUserExistsRequest request,
            ServerCallContext context
        )
        {
            var command = new CheckUserExistsCommand()
            {
                Email = request.Emails.ToList()
            };
            var result = await _mediator.Send(command);

            return result;
        }

        public override async Task<CheckUserExistsResponse> CheckUserIdExists(
            CheckUserIdExistsRequest request,
            ServerCallContext context
        )
        {
            var command = new CheckUserIdExistsCommand()
            {
                UserIds = request.UserIds.ToList()
            };
            var result = await _mediator.Send(command);

            return result;
        }

        public override async Task<CheckUserExistsResponse> EnsureUsersExist(
            EnsureUsersRequest request,
            ServerCallContext context
        )
        {
            var command = new EnsureUsersExistCommand
            {
                Emails = request.Emails.ToList()
            };
            var result = await _mediator.Send(command, context.CancellationToken);
            return result;
        }
    }
}
