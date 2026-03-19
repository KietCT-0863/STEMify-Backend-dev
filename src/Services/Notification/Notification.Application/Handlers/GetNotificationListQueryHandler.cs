using MediatR;
using Notification.Application.Common.Interfaces;
using Notification.Application.Queries;
using Shared.Protos.Notification;

namespace Notification.Application.Handlers
{
    public class GetNotificationListQueryHandler
        : IRequestHandler<GetNotificationListQuery, NotificationList>
    {
        private readonly INotificationUnitOfWork _unitOfWork;

        public GetNotificationListQueryHandler(INotificationUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<NotificationList> Handle(
            GetNotificationListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var notifications = await _unitOfWork.Notifications.GetAllAsync(cancellationToken);

                var list = new NotificationList();
                foreach (var notification in notifications)
                {
                    var response = new NotificationResponse
                    {
                        Id = notification.Id,
                        Title = notification.Title,
                        Message = notification.Message,
                        ClickUrl = notification.ClickUrl,
                        IsRead = notification.IsRead,
                        UserId = notification.UserId,
                        CreatedDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                            notification.CreatedDate
                        ),
                        LastModifiedDate =
                            notification.LastModifiedDate != null
                                ? Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(
                                    notification.LastModifiedDate.Value
                                )
                                : null,
                    };

                    list.Notifications.Add(response);
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the notification list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
