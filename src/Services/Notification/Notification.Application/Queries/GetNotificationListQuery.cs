using MediatR;
using Shared.Protos.Notification;

namespace Notification.Application.Queries
{
    public class GetNotificationListQuery : IRequest<NotificationList> { }
}
