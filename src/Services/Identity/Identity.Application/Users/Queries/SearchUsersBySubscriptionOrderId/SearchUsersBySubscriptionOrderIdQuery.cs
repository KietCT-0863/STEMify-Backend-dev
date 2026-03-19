using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.SearchUsersBySubscriptionOrderId;

public class SearchUsersBySubscriptionOrderIdQuery : IRequest<PagedResult<UserSummaryDto>>
{
    public int OrganizationId { get; set; }
    public int SubscriptionOrderId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


