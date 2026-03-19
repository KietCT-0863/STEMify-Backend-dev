using Grpc.Core;
using Identity.Application.Groups.Commands.AddStudentsToGroup;
using Identity.Application.Groups.Commands.CreateGroup;
using Identity.Application.Groups.Commands.CreateGroupWithStudents;
using Identity.Application.Groups.Commands.DeleteGroup;
using Identity.Application.Groups.Commands.RemoveStudentsFromGroup;
using Identity.Application.Groups.Commands.UpdateGroup;
using Identity.Application.Groups.Queries.GetGroupById;
using Identity.Application.Groups.Queries.GetGroupList;
using MediatR;
using Shared.Protos.User;

namespace Identity.API.Services;

public class GroupGrpcService : GrpcGroup.GrpcGroupBase
{
    private readonly IMediator _mediator;

    public GroupGrpcService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<GetGroupListResponse> GetGroupList(
        GetGroupListRequest request,
        ServerCallContext context)
    {
        if (request.OrganizationId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationId must be greater than zero."));
        }

        var query = new GetGroupListQuery
        {
            OrganizationId = request.OrganizationId,
            IncludeArchived = request.IncludeArchived,
            PageNumber = request.PageNumber > 0 ? request.PageNumber : 1,
            PageSize = request.PageSize > 0 ? request.PageSize : 100,
            Grade = request.Grade > 0 ? request.Grade : null
        };

        var result = await _mediator.Send(query, context.CancellationToken);

        var response = new GetGroupListResponse
        {
            PageNumber = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };

        response.Items.AddRange(
            result.Items.Select(group =>
            {
                var item = new GroupListItem
                {
                    Id = group.Id,
                    OrganizationId = group.OrganizationId,
                    Name = group.Name,
                    Description = group.Description,
                    Code = group.Code,
                    Status = group.Status,
                    StudentCount = group.StudentCount,
                    CreatedAt = group.CreatedAt.ToString("O"),
                    UpdatedAt = group.UpdatedAt.ToString("O")
                };

                item.Students.AddRange(
                    group.Students.Select(student => new GroupStudent
                    {
                        OrganizationUserId = student.OrganizationUserId.ToString(),
                        UserId = student.UserId.ToString(),
                        Email = student.Email,
                        UserName = student.UserName,
                        FullName = student.FullName,
                        SubscriptionOrderId = student.SubscriptionOrderId ?? 0,
                        JoinedAt = student.JoinedAt.ToString("O"),
                        IsActive = student.IsActive,
                    }));

                return item;
            }));

        return response;
    }

    public override async Task<GetGroupByIdResponse> GetGroupById(
        GetGroupByIdRequest request,
        ServerCallContext context)
    {
        if (request.GroupId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "GroupId must be greater than zero."));
        }

        var query = new GetGroupByIdQuery
        {
            GroupId = request.GroupId,
            ActiveOnly = request.ActiveOnly,
            SubscriptionOrderId = request.SubscriptionOrderId > 0 ? request.SubscriptionOrderId : null
        };

        var result = await _mediator.Send(query, context.CancellationToken);

        var response = new GetGroupByIdResponse
        {
            Id = result.Id,
            OrganizationId = result.OrganizationId,
            Name = result.Name,
            Description = result.Description,
            Code = result.Code,
            Status = result.Status.ToString(),
            CreatedByUserId = result.CreatedByUserId.ToString(),
            CreatedAt = result.CreatedAt.ToString("O"),
            UpdatedAt = result.UpdatedAt.ToString("O"),
            StudentCount = result.StudentCount,
            TotalStudentCount = result.TotalStudentCount
        };

        if (result.FilteredSubscriptionOrderId.HasValue)
        {
            response.FilteredSubscriptionOrderId = result.FilteredSubscriptionOrderId.Value;
        }

        response.Students.AddRange(
            result.Students.Select(student => new GroupStudent
            {
                OrganizationUserId = student.OrganizationUserId.ToString(),
                UserId = student.UserId.ToString(),
                Email = student.Email,
                UserName = student.UserName,
                FullName = student.FullName,
                SubscriptionOrderId = student.SubscriptionOrderId ?? 0,
                JoinedAt = student.JoinedAt.ToString("O"),
                IsActive = student.IsActive,
            }));

        return response;
    }

    public override async Task<GroupResponse> CreateGroup(
        CreateGroupRequest request,
        ServerCallContext context)
    {
        if (request.OrganizationId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationId must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Group name is required."));
        }

        if (!Guid.TryParse(request.CreatedByUserId, out var createdByUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid CreatedByUserId format."));
        }

        var command = new CreateGroupCommand
        {
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Description = request.Description,
            Code = request.Code,
            CreatedByUserId = createdByUserId
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new GroupResponse
        {
            Id = result.Id,
            OrganizationId = result.OrganizationId,
            Name = result.Name,
            Description = result.Description,
            Code = result.Code,
            Status = result.Status.ToString(),
            CreatedByUserId = result.CreatedByUserId.ToString(),
            CreatedAt = result.CreatedAt.ToString("O"),
            UpdatedAt = result.UpdatedAt.ToString("O")
        };
    }

    public override async Task<GroupResponse> UpdateGroup(
        UpdateGroupRequest request,
        ServerCallContext context)
    {
       
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Group name is required."));
        }

        var command = new UpdateGroupCommand
        {
            GroupId = request.GroupId,
            Name = request.Name,
            Description = request.Description
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new GroupResponse
        {
            Id = result.Id,
            OrganizationId = result.OrganizationId,
            Name = result.Name,
            Description = result.Description,
            Code = result.Code,
            Status = result.Status.ToString(),
            CreatedByUserId = result.CreatedByUserId.ToString(),
            CreatedAt = result.CreatedAt.ToString("O"),
            UpdatedAt = result.UpdatedAt.ToString("O")
        };
    }

    public override async Task<DeleteGroupResponse> DeleteGroup(
        DeleteGroupRequest request,
        ServerCallContext context)
    {
        if (request.GroupId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "GroupId must be greater than zero."));
        }

        var command = new DeleteGroupCommand
        {
            GroupId = request.GroupId
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new DeleteGroupResponse
        {
            IsSuccess = result
        };
    }

    public override async Task<AddStudentsToGroupResponse> AddStudentsToGroup(
        AddStudentsToGroupRequest request,
        ServerCallContext context)
    {
        if (request.GroupId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "GroupId must be greater than zero."));
        }

        if (request.StudentIds == null || request.StudentIds.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one student ID is required."));
        }

        var studentIds = request.StudentIds
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();

        if (studentIds.Count != request.StudentIds.Count)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Some student IDs have invalid format."));
        }

        var command = new AddStudentsToGroupCommand
        {
            GroupId = request.GroupId,
            StudentIds = studentIds
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new AddStudentsToGroupResponse
        {
            IsSuccess = result
        };
    }

    public override async Task<RemoveStudentsFromGroupResponse> RemoveStudentsFromGroup(
        RemoveStudentsFromGroupRequest request,
        ServerCallContext context)
    {
        if (request.GroupId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "GroupId must be greater than zero."));
        }

        if (request.StudentIds == null || request.StudentIds.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "At least one student ID is required."));
        }

        var studentIds = request.StudentIds
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();

        if (studentIds.Count != request.StudentIds.Count)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Some student IDs have invalid format."));
        }

        var command = new RemoveStudentsFromGroupCommand
        {
            GroupId = request.GroupId,
            StudentIds = studentIds
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new RemoveStudentsFromGroupResponse
        {
            IsSuccess = result
        };
    }

    public override async Task<GroupResponse> CreateGroupWithStudents(
        CreateGroupWithStudentsRequest request,
        ServerCallContext context)
    {
        if (request.OrganizationId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "OrganizationId must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Group name is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Group code is required."));
        }

        if (!Guid.TryParse(request.CreatedByUserId, out var createdByUserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid CreatedByUserId format."));
        }

        var studentIds = new List<Guid>();
        if (request.StudentIds != null && request.StudentIds.Count > 0)
        {
            studentIds = request.StudentIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(Guid.Parse)
                .ToList();

            if (studentIds.Count != request.StudentIds.Count)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Some student IDs have invalid format."));
            }
        }

        var command = new CreateGroupWithStudentsCommand
        {
            OrganizationId = request.OrganizationId,
            Name = request.Name,
            Code = request.Code,
            Grade = request.Grade > 0 ? request.Grade : null,
            Description = request.Description,
            CreatedByUserId = createdByUserId,
            StudentIds = studentIds,
            SubscriptionOrderId = request.SubscriptionOrderId.HasValue && request.SubscriptionOrderId.Value > 0 ? request.SubscriptionOrderId.Value : null,
            LicenseType = !string.IsNullOrWhiteSpace(request.LicenseType) ? request.LicenseType : "Student"
        };

        var result = await _mediator.Send(command, context.CancellationToken);

        return new GroupResponse
        {
            Id = result.Id,
            OrganizationId = result.OrganizationId,
            Name = result.Name,
            Description = result.Description,
            Code = result.Code,
            Status = result.Status.ToString(),
            CreatedByUserId = result.CreatedByUserId.ToString(),
            CreatedAt = result.CreatedAt.ToString("O"),
            UpdatedAt = result.UpdatedAt.ToString("O")
        };
    }
}

