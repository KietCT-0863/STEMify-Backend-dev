using MediatR;
using Notification.Application.Commands;
using Notification.Application.Common.Interfaces;

namespace Notification.Application.Handlers
{
    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand>
    {
        private readonly INotificationUnitOfWork _unitOfWork;

        public DeleteNotificationCommandHandler(INotificationUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeleteNotificationCommand request,
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

                await _unitOfWork.Notifications.DeleteAsync(notification, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while deleting the notification: {ex.Message}",
                    ex
                );
            }
        }
    }
}
