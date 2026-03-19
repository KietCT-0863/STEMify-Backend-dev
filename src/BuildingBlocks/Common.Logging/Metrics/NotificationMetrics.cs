using System.Diagnostics.Metrics;

namespace Common.Logging.Metrics;

/// <summary>
/// Metrics for Notification Service - Email, SMS, and Push notification delivery
/// Following OpenTelemetry semantic conventions and best practices
/// </summary>
public static class NotificationMetrics
{
    private static readonly Meter Meter = new("STEMify.Notification", "1.0.0");

    #region Notification Delivery Metrics

    /// <summary>
    /// Total number of notifications sent
    /// Labels: notification_type (email, sms, push), status (sent, failed, queued)
    /// </summary>
    public static readonly Counter<long> NotificationsSent = Meter.CreateCounter<long>(
        "notification.sent.total",
        unit: "{notifications}",
        description: "Total number of notifications sent"
    );

    /// <summary>
    /// Duration of notification delivery
    /// Labels: notification_type, provider, status
    /// </summary>
    public static readonly Histogram<double> DeliveryDuration = Meter.CreateHistogram<double>(
        "notification.delivery.duration",
        unit: "s",
        description: "Duration of notification delivery in seconds"
    );

    /// <summary>
    /// Total number of notification delivery confirmations
    /// Labels: notification_type, delivery_status (delivered, bounced, opened, clicked)
    /// </summary>
    public static readonly Counter<long> DeliveryConfirmations = Meter.CreateCounter<long>(
        "notification.delivery.confirmations.total",
        unit: "{confirmations}",
        description: "Total number of notification delivery confirmations"
    );

    #endregion

    #region Email Metrics

    /// <summary>
    /// Total number of emails sent
    /// Labels: email_type (verification, password_reset, invitation, marketing), status
    /// </summary>
    public static readonly Counter<long> EmailsSent = Meter.CreateCounter<long>(
        "notification.email.sent.total",
        unit: "{emails}",
        description: "Total number of emails sent"
    );

    /// <summary>
    /// Total number of email bounces
    /// Labels: bounce_type (hard, soft, complaint)
    /// </summary>
    public static readonly Counter<long> EmailBounces = Meter.CreateCounter<long>(
        "notification.email.bounces.total",
        unit: "{bounces}",
        description: "Total number of email bounces"
    );

    /// <summary>
    /// Email engagement metrics
    /// Labels: engagement_type (opened, clicked, unsubscribed)
    /// </summary>
    public static readonly Counter<long> EmailEngagement = Meter.CreateCounter<long>(
        "notification.email.engagement.total",
        unit: "{events}",
        description: "Total number of email engagement events"
    );

    #endregion

    #region SMS Metrics

    /// <summary>
    /// Total number of SMS sent
    /// Labels: sms_type (verification, otp, alert), provider (twilio, vonage), status
    /// </summary>
    public static readonly Counter<long> SmsSent = Meter.CreateCounter<long>(
        "notification.sms.sent.total",
        unit: "{sms}",
        description: "Total number of SMS messages sent"
    );

    /// <summary>
    /// Total SMS cost
    /// Labels: provider, country_code
    /// </summary>
    public static readonly Counter<decimal> SmsCost = Meter.CreateCounter<decimal>(
        "notification.sms.cost.total",
        unit: "USD",
        description: "Total SMS cost in USD"
    );

    #endregion

    #region Push Notification Metrics

    /// <summary>
    /// Total number of push notifications sent
    /// Labels: platform (ios, android, web), notification_type, status
    /// </summary>
    public static readonly Counter<long> PushNotificationsSent = Meter.CreateCounter<long>(
        "notification.push.sent.total",
        unit: "{notifications}",
        description: "Total number of push notifications sent"
    );

    /// <summary>
    /// Total number of push notification interactions
    /// Labels: platform, interaction_type (opened, dismissed, action_taken)
    /// </summary>
    public static readonly Counter<long> PushInteractions = Meter.CreateCounter<long>(
        "notification.push.interactions.total",
        unit: "{interactions}",
        description: "Total number of push notification interactions"
    );

    #endregion

    #region Queue Metrics

    /// <summary>
    /// Current size of notification queue
    /// Labels: notification_type
    /// </summary>
    public static readonly ObservableGauge<int> QueueSize = Meter.CreateObservableGauge(
        "notification.queue.size",
        () => GetQueueSize(),
        unit: "{notifications}",
        description: "Current size of notification queue"
    );

    /// <summary>
    /// Age of oldest notification in queue
    /// Labels: notification_type
    /// </summary>
    public static readonly ObservableGauge<double> QueueAge = Meter.CreateObservableGauge(
        "notification.queue.age",
        () => GetQueueAge(),
        unit: "s",
        description: "Age of oldest notification in queue in seconds"
    );

    private static Func<int> _queueSizeProvider = () => 0;
    private static Func<double> _queueAgeProvider = () => 0;

    public static void SetQueueSizeProvider(Func<int> provider)
    {
        _queueSizeProvider = provider;
    }

    public static void SetQueueAgeProvider(Func<double> provider)
    {
        _queueAgeProvider = provider;
    }

    private static int GetQueueSize() => _queueSizeProvider();
    private static double GetQueueAge() => _queueAgeProvider();

    #endregion

    #region Provider Metrics

    /// <summary>
    /// Total number of provider API calls
    /// Labels: provider (sendgrid, mailgun, twilio), operation, status
    /// </summary>
    public static readonly Counter<long> ProviderApiCalls = Meter.CreateCounter<long>(
        "notification.provider.api.calls.total",
        unit: "{calls}",
        description: "Total number of notification provider API calls"
    );

    /// <summary>
    /// Duration of provider API calls
    /// Labels: provider, operation, status
    /// </summary>
    public static readonly Histogram<double> ProviderApiDuration = Meter.CreateHistogram<double>(
        "notification.provider.api.duration",
        unit: "s",
        description: "Duration of provider API calls in seconds"
    );

    /// <summary>
    /// Total number of provider errors
    /// Labels: provider, error_type (rate_limit, timeout, invalid_credentials)
    /// </summary>
    public static readonly Counter<long> ProviderErrors = Meter.CreateCounter<long>(
        "notification.provider.errors.total",
        unit: "{errors}",
        description: "Total number of provider errors"
    );

    #endregion

    #region Helper Methods

    public static void RecordNotificationSent(string notificationType, string status, TimeSpan duration, string? provider = null)
    {
        NotificationsSent.Add(1,
            new KeyValuePair<string, object?>("notification_type", notificationType),
            new KeyValuePair<string, object?>("status", status));

        var tags = new List<KeyValuePair<string, object?>>
        {
            new("notification_type", notificationType),
            new("status", status)
        };

        if (!string.IsNullOrEmpty(provider))
        {
            tags.Add(new("provider", provider));
        }

        DeliveryDuration.Record(duration.TotalSeconds, tags.ToArray());
    }

    public static void RecordEmailSent(string emailType, string status)
    {
        EmailsSent.Add(1,
            new KeyValuePair<string, object?>("email_type", emailType),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordEmailBounce(string bounceType)
    {
        EmailBounces.Add(1,
            new KeyValuePair<string, object?>("bounce_type", bounceType));
    }

    public static void RecordEmailEngagement(string engagementType)
    {
        EmailEngagement.Add(1,
            new KeyValuePair<string, object?>("engagement_type", engagementType));
    }

    public static void RecordSmsSent(string smsType, string provider, string status, decimal cost)
    {
        SmsSent.Add(1,
            new KeyValuePair<string, object?>("sms_type", smsType),
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("status", status));

        if (cost > 0)
        {
            SmsCost.Add(cost,
                new KeyValuePair<string, object?>("provider", provider));
        }
    }

    public static void RecordPushNotificationSent(string platform, string notificationType, string status)
    {
        PushNotificationsSent.Add(1,
            new KeyValuePair<string, object?>("platform", platform),
            new KeyValuePair<string, object?>("notification_type", notificationType),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordProviderApiCall(string provider, string operation, string status, TimeSpan duration)
    {
        ProviderApiCalls.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));

        ProviderApiDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordProviderError(string provider, string errorType)
    {
        ProviderErrors.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    #endregion
}
