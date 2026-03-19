using MediatR;

namespace Identity.Application.Groups.Queries.GetGroupById;

public class GetGroupByIdQuery : IRequest<GroupDetailDto>
{
    public int GroupId { get; set; }
    public bool ActiveOnly { get; set; }
    public int? SubscriptionOrderId { get; set; }
}

