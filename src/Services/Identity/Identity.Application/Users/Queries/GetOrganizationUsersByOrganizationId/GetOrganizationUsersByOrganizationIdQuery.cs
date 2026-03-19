using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;

public class GetOrganizationUsersByOrganizationIdQuery : IRequest<PagedResult<OrganizationUserGroupedDto>>
{
    public int OrganizationId { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public bool GroupByUser { get; set; } = true; 
    public string Role { get; set; } = string.Empty;
    public string SubscriptionOrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Search { get; set; } = string.Empty;
    public int? GroupId { get; set; }
}

