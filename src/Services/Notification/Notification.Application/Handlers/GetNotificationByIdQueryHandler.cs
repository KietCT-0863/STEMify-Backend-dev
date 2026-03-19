using MediatR;
using Notification.Application.Common.Interfaces;
using Notification.Application.Queries;
using Shared.Protos.Notification;

namespace Notification.Application.Handlers
{
    public class GetNotificationByIdQueryHandler
        : IRequestHandler<GetNotificationByIdQuery, NotificationResponse>
    {
        private readonly INotificationUnitOfWork _unitOfWork;

        public GetNotificationByIdQueryHandler(INotificationUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<NotificationResponse> Handle(
            GetNotificationByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var notification = await _unitOfWork.Notifications.FindByIdAsync(
                    request.Id,
                    cancellationToken
                );

                if (notification == null)
                    throw new KeyNotFoundException($"Notification with ID {request.Id} not found.");

                var response = new NotificationResponse
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    UserId = notification.UserId,
                    IsRead = notification.IsRead,
                    ClickUrl = notification.ClickUrl,
                    Message = notification.Message,
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

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the notification: {ex.Message}",
                    ex
                );
            }
        }
    }
}
