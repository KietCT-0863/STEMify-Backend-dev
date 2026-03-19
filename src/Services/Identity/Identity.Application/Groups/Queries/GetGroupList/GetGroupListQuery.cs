using Identity.Application.Common.Models;
using MediatR;

namespace Identity.Application.Groups.Queries.GetGroupList;

public class GetGroupListQuery : IRequest<PagedResult<GroupListItemDto>>
{
    public int OrganizationId { get; set; }
    public bool IncludeArchived { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public int? Grade { get; set; }
}

