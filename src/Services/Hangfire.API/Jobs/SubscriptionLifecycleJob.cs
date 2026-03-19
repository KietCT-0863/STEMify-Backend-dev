using Microsoft.Extensions.Logging;
using Order.Application.Services;

namespace Hangfire.API.Jobs
{
    public class SubscriptionLifecycleJob
    {
        private readonly ISubscriptionLifecycleService _subscriptionLifecycleService;
        private readonly ILogger<SubscriptionLifecycleJob> _logger;

        public SubscriptionLifecycleJob(
            ISubscriptionLifecycleService subscriptionLifecycleService,
            ILogger<SubscriptionLifecycleJob> logger)
        {
            _subscriptionLifecycleService = subscriptionLifecycleService;
            _logger = logger;
        }

        public async Task ActivatePendingSubscriptionsAsync()
        {
            _logger.LogInformation("SubscriptionLifecycleJob: starting activation of pending subscriptions");

            try
            {
                await _subscriptionLifecycleService.ActivatePendingSubscriptionsAsync(CancellationToken.None);
                _logger.LogInformation("SubscriptionLifecycleJob: completed activation of pending subscriptions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionLifecycleJob: error activating pending subscriptions");
                throw;
            }
        }

        public async Task ExpireEndedSubscriptionsAsync()
        {
            _logger.LogInformation("SubscriptionLifecycleJob: starting expiration of ended subscriptions");

            try
            {
                await _subscriptionLifecycleService.ExpireEndedSubscriptionsAsync(CancellationToken.None);
                _logger.LogInformation("SubscriptionLifecycleJob: completed expiration of ended subscriptions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionLifecycleJob: error expiring subscriptions");
                throw;
            }
        }

        public async Task CheckExpiringSubscriptions30DaysAsync()
        {
            await ExecuteExpiryCheckAsync(30);
        }

        public async Task CheckExpiringSubscriptions7DaysAsync()
        {
            await ExecuteExpiryCheckAsync(7);
        }

        public async Task CheckExpiringSubscriptions1DayAsync()
        {
            await ExecuteExpiryCheckAsync(1);
        }

        private async Task ExecuteExpiryCheckAsync(int warningDays)
        {
            _logger.LogInformation(
                "SubscriptionLifecycleJob: starting expiry check with warning window of {WarningDays} days",
                warningDays);

            try
            {
                await _subscriptionLifecycleService.CheckAndNotifyExpiringSubscriptionsAsync(
                    warningDays,
                    CancellationToken.None);

                _logger.LogInformation(
                    "SubscriptionLifecycleJob: completed expiry check with warning window of {WarningDays} days",
                    warningDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "SubscriptionLifecycleJob: error during expiry check with warning window of {WarningDays} days",
                    warningDays);
                throw;
            }
        }
    }
}

