using FirebaseAdmin.Messaging;
using Infrastructure.Resilience;
using Microsoft.Extensions.Logging;
using Notification.Application.Common.Interfaces.Services;
using Notification.Domain.Entities;
using Polly.CircuitBreaker;

namespace Notification.Infrastructure.Services
{
    public class FCMNotificationService : IFCMNotificationService
    {
        private readonly FirebaseMessaging _messaging;
        private readonly IPollyResilienceService _resilienceService;
        private readonly ILogger<FCMNotificationService> _logger;
        private const string FCM_POLICY_NAME = "FCMNotificationPolicy";

        public FCMNotificationService(
            IPollyResilienceService resilienceService,
            ILogger<FCMNotificationService> logger
        )
        {
            _messaging = FirebaseMessaging.DefaultInstance;
            _resilienceService = resilienceService;
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(FCMessage fcMessage)
        {
            try
            {
                var result = await _resilienceService.ExecuteAsync(
                    async (cancellationToken) =>
                    {
                        return await SendToFirebaseAsync(fcMessage, cancellationToken);
                    },
                    FCM_POLICY_NAME
                );

                _logger.LogInformation(
                    "FCM notification sent successfully to {TokenCount} devices",
                    fcMessage.tokens?.Count ?? 0
                );

                return result;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(
                    "FCM service circuit breaker is open, notification not sent: {Message}",
                    ex.Message
                );

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification");
                return false;
            }
        }

        private async Task<bool> SendToFirebaseAsync(
            FCMessage fcMessage,
            CancellationToken cancellationToken
        )
        {
            if (fcMessage.tokens == null || !fcMessage.tokens.Any())
            {
                _logger.LogWarning("No FCM tokens provided for notification");
                return false;
            }

            // Multicast message to multiple devices
            var message = new MulticastMessage()
            {
                Tokens = fcMessage.tokens,
                Notification = new FirebaseAdmin.Messaging.Notification()
                {
                    Title = fcMessage.title,
                    Body = fcMessage.body,
                },
                Data = fcMessage.data,
            };

            // Send to Firebase với cancellation token support
            var response = await _messaging.SendEachForMulticastAsync(message, cancellationToken);

            _logger.LogDebug(
                "FCM response - Success: {SuccessCount}, Failure: {FailureCount}",
                response.SuccessCount,
                response.FailureCount
            );

            if (response.FailureCount > 0)
            {
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    var singleResponse = response.Responses[i];
                    if (!singleResponse.IsSuccess)
                    {
                        _logger.LogWarning(
                            "FCM failed for token {TokenIndex}: {Error}",
                            i,
                            singleResponse.Exception?.Message
                        );
                    }
                }

                return response.SuccessCount > 0;
            }

            return true;
        }
    }
}
