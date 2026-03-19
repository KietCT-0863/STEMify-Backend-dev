using MediatR;
using Notification.Application.Commands;
using Notification.Application.Common.Interfaces;
using Shared.Exceptions;
using Shared.Protos.Notification;

namespace Notification.Application.Handlers
{
    public class UpdateNotificationCommandHandler
        : IRequestHandler<UpdateNotificationCommand, NotificationResponse>
    {
        private readonly INotificationUnitOfWork _unitOfWork;

        public UpdateNotificationCommandHandler(INotificationUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<NotificationResponse> Handle(
            UpdateNotificationCommand request,
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
                    throw new NotFoundException($"Notification with ID {request.Id} not found.");

                notification.IsRead = request.IsRead;
                notification.LastModifiedDate = DateTimeOffset.UtcNow;

                await _unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var response = new NotificationResponse
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    ClickUrl = notification.ClickUrl,
                    Message = notification.Message,
                    IsRead = notification.IsRead,
                    UserId = notification.UserId.ToString(),
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
                    $"An error occurred while updating the notification: {ex.Message}",
                    ex
                );
            }
        }
    }
}
