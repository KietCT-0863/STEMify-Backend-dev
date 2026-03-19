using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Models;
using Identity.Application.Common.Models.Users;
using MediatR;

namespace Identity.Application.Users.Queries.SearchUsersBySubscriptionOrderId;

public class SearchUsersBySubscriptionOrderIdQueryHandler
    : IRequestHandler<SearchUsersBySubscriptionOrderIdQuery, PagedResult<UserSummaryDto>>
{
    public async Task<PagedResult<UserSummaryDto>> Handle(
        SearchUsersBySubscriptionOrderIdQuery request,
        CancellationToken cancellationToken)
    {
        // Deprecated: search by subscription should be done via Order/License context or unified SearchUsersQuery.
        throw new NotSupportedException("SearchUsersBySubscriptionOrderIdQuery is deprecated. Use SearchUsersQuery with SubscriptionOrderId filter instead.");
    }
}


