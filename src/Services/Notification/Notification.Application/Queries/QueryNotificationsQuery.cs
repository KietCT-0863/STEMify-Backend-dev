using MediatR;
using Shared.Protos.Notification;

namespace Notification.Application.Queries
{
    public class QueryNotificationsQuery : IRequest<PagedNotificationList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public string? UserId { get; set; }
        public bool? IsRead { get; set; }
    }
}
