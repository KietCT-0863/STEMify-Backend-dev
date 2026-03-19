using System.Security.Cryptography;
using System.Text;
using Infrastructure.Idempotency;
using MediatR;
using Microsoft.Extensions.Logging;
using Notification.Application.Commands;
using Notification.Application.Common.Interfaces;
using Shared.Protos.Notification;

namespace Notification.Application.Handlers
{
    public class CreateNotificationCommandHandler
        : IRequestHandler<CreateNotificationCommand, NotificationResponse>
    {
        private readonly INotificationUnitOfWork _unitOfWork;
        private readonly IIdempotencyService _idempotencyService;
        private readonly ILogger<CreateNotificationCommandHandler> _logger;

        public CreateNotificationCommandHandler(
            INotificationUnitOfWork unitOfWork,
            IIdempotencyService idempotencyService,
            ILogger<CreateNotificationCommandHandler> logger
        )
        {
            _unitOfWork = unitOfWork;
            _idempotencyService = idempotencyService;
            _logger = logger;
        }

        public async Task<NotificationResponse> Handle(
            CreateNotificationCommand request,
            CancellationToken cancellationToken
        )
        {
            // Tạo idempotency key từ nội dung request
            var idempotencyKey = GenerateIdempotencyKey(request);

            _logger.LogDebug(
                "Processing create notification request with idempotency key: {IdempotencyKey}",
                idempotencyKey
            );

            return await _idempotencyService.ExecuteAsync(
                idempotencyKey,
                async (ct) => await CreateNotificationInternalAsync(request, ct),
                expiration: TimeSpan.FromHours(24),
                cancellationToken: cancellationToken
            );
        }

        private async Task<NotificationResponse> CreateNotificationInternalAsync(
            CreateNotificationCommand request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                _logger.LogInformation(
                    "Creating new notification for user {UserId} with title: {Title}",
                    request.UserId,
                    request.Title
                );

                var notification = new Domain.Entities.Notification
                {
                    Title = request.Title,
                    UserId = request.UserId,
                    Message = request.Message,
                    ClickUrl = request.ClickUrl,
                };

                await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Notification created successfully with ID: {NotificationId}",
                    notification.Id
                );

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

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create notification for user {UserId}",
                    request.UserId
                );

                throw new ApplicationException(
                    $"An error occurred while creating the notification: {ex.Message}",
                    ex
                );
            }
        }

        /// <summary>
        /// Tạo idempotency key từ nội dung request
        /// Giải thích: Sử dụng hash của các fields quan trọng để tạo key duy nhất
        /// </summary>
        private static string GenerateIdempotencyKey(CreateNotificationCommand request)
        {
            var keySource =
                $"{request.UserId}|{request.Title}|{request.Message}|{request.ClickUrl}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource));
            var hashString = Convert.ToHexString(hashBytes);

            return $"create_notification_{hashString}";
        }
    }
}
