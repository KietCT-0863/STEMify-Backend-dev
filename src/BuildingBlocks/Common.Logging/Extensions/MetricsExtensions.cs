using System.Diagnostics.Metrics;

namespace Common.Logging.Extensions;

public static class MetricsExtensions
{
    private static readonly Meter _meter = new("STEMifyBackend", "1.0.0");

    // Business Metrics
    public static readonly Counter<long> UserRegistrations = _meter.CreateCounter<long>(
        "user_registrations_total",
        "Total number of user registrations"
    );

    public static readonly Counter<long> UserLogins = _meter.CreateCounter<long>(
        "user_logins_total",
        "Total number of user logins"
    );

    public static readonly Counter<long> OrdersCreated = _meter.CreateCounter<long>(
        "orders_created_total",
        "Total number of orders created"
    );

    public static readonly Counter<long> OrdersCompleted = _meter.CreateCounter<long>(
        "orders_completed_total",
        "Total number of orders completed"
    );

    public static readonly Counter<long> OrdersFailed = _meter.CreateCounter<long>(
        "orders_failed_total",
        "Total number of failed orders"
    );

    public static readonly Counter<long> PaymentsProcessed = _meter.CreateCounter<long>(
        "payments_processed_total",
        "Total number of payments processed"
    );

    public static readonly Counter<long> PaymentsFailed = _meter.CreateCounter<long>(
        "payments_failed_total",
        "Total number of failed payments"
    );

    public static readonly Counter<long> ResourcesCreated = _meter.CreateCounter<long>(
        "resources_created_total",
        "Total number of resources created"
    );

    public static readonly Counter<long> ClassroomSessions = _meter.CreateCounter<long>(
        "classroom_sessions_total",
        "Total number of classroom sessions"
    );

    public static readonly Counter<long> NotificationsSent = _meter.CreateCounter<long>(
        "notifications_sent_total",
        "Total number of notifications sent"
    );

    // Performance Metrics
    public static readonly Histogram<double> DatabaseQueryDuration = _meter.CreateHistogram<double>(
        "database_query_duration_seconds",
        "Duration of database queries in seconds"
    );

    public static readonly Histogram<double> ExternalApiCallDuration = _meter.CreateHistogram<double>(
        "external_api_call_duration_seconds",
        "Duration of external API calls in seconds"
    );

    public static readonly Histogram<double> BusinessOperationDuration = _meter.CreateHistogram<double>(
        "business_operation_duration_seconds",
        "Duration of business operations in seconds"
    );

    // System Metrics - ObservableGauge cần function để lấy giá trị
    private static long _activeUsersCount = 0;
    private static long _pendingOrdersCount = 0;
    private static long _queueSizeCount = 0;

    public static readonly ObservableGauge<long> ActiveUsers = _meter.CreateObservableGauge<long>(
        "active_users",
        () => _activeUsersCount,
        "Number of currently active users"
    );

    public static readonly ObservableGauge<long> PendingOrders = _meter.CreateObservableGauge<long>(
        "pending_orders",
        () => _pendingOrdersCount,
        "Number of pending orders"
    );

    public static readonly ObservableGauge<long> QueueSize = _meter.CreateObservableGauge<long>(
        "queue_size",
        () => _queueSizeCount,
        "Current size of message queue"
    );

    // Error Metrics
    public static readonly Counter<long> DatabaseErrors = _meter.CreateCounter<long>(
        "database_errors_total",
        "Total number of database errors"
    );

    public static readonly Counter<long> ExternalApiErrors = _meter.CreateCounter<long>(
        "external_api_errors_total",
        "Total number of external API errors"
    );

    public static readonly Counter<long> ValidationErrors = _meter.CreateCounter<long>(
        "validation_errors_total",
        "Total number of validation errors"
    );

    // Helper methods for common operations
    // FIXED: Removed high-cardinality labels (user_id, order_id, payment_id) to prevent cardinality explosion
    // Best Practice: Only use low-cardinality labels (user_type, order_type, payment_method, etc.)
    public static void RecordUserRegistration(string userType)
    {
        UserRegistrations.Add(1, new KeyValuePair<string, object?>("user_type", userType));
    }

    public static void RecordUserLogin(string loginMethod, string status = "success")
    {
        UserLogins.Add(1, new KeyValuePair<string, object?>("login_method", loginMethod),
                             new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordOrderCreated(string orderType)
    {
        OrdersCreated.Add(1, new KeyValuePair<string, object?>("order_type", orderType));
    }

    public static void RecordOrderCompleted(string orderType)
    {
        OrdersCompleted.Add(1, new KeyValuePair<string, object?>("order_type", orderType));
    }

    public static void RecordOrderFailed(string orderType, string reason)
    {
        OrdersFailed.Add(1, new KeyValuePair<string, object?>("order_type", orderType),
                               new KeyValuePair<string, object?>("failure_reason", reason));
    }

    public static void RecordPaymentProcessed(string paymentMethod, string currency = "VND")
    {
        PaymentsProcessed.Add(1, new KeyValuePair<string, object?>("payment_method", paymentMethod),
                                    new KeyValuePair<string, object?>("currency", currency));
    }

    public static void RecordPaymentFailed(string paymentMethod, string reason)
    {
        PaymentsFailed.Add(1, new KeyValuePair<string, object?>("payment_method", paymentMethod),
                                 new KeyValuePair<string, object?>("failure_reason", reason));
    }

    public static void RecordDatabaseQuery(string operation, TimeSpan duration, bool success)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("operation", operation),
            new("success", success.ToString())
        };

        DatabaseQueryDuration.Record(duration.TotalSeconds, tags.ToArray());

        if (!success)
        {
            DatabaseErrors.Add(1, tags.ToArray());
        }
    }

    public static void RecordExternalApiCall(string apiName, string endpoint, TimeSpan duration, bool success)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("api_name", apiName),
            new("endpoint", endpoint),
            new("success", success.ToString())
        };

        ExternalApiCallDuration.Record(duration.TotalSeconds, tags.ToArray());

        if (!success)
        {
            ExternalApiErrors.Add(1, tags.ToArray());
        }
    }

    public static void RecordBusinessOperation(string operation, TimeSpan duration, bool success)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("operation", operation),
            new("success", success.ToString())
        };

        BusinessOperationDuration.Record(duration.TotalSeconds, tags.ToArray());
    }

    public static void RecordValidationError(string field, string errorType)
    {
        ValidationErrors.Add(1, new KeyValuePair<string, object?>("field", field),
                                   new KeyValuePair<string, object?>("error_type", errorType));
    }

    // Helper methods for ObservableGauge values
    public static void UpdateActiveUsersCount(long count)
    {
        _activeUsersCount = count;
    }

    public static void UpdatePendingOrdersCount(long count)
    {
        _pendingOrdersCount = count;
    }

    public static void UpdateQueueSize(long size)
    {
        _queueSizeCount = size;
    }

    // Increment/Decrement helpers for observable gauges
    public static void IncrementActiveUsers()
    {
        Interlocked.Increment(ref _activeUsersCount);
    }

    public static void DecrementActiveUsers()
    {
        Interlocked.Decrement(ref _activeUsersCount);
    }

    public static void IncrementPendingOrders()
    {
        Interlocked.Increment(ref _pendingOrdersCount);
    }

    public static void DecrementPendingOrders()
    {
        Interlocked.Decrement(ref _pendingOrdersCount);
    }

    public static void IncrementQueueSize()
    {
        Interlocked.Increment(ref _queueSizeCount);
    }

    public static void DecrementQueueSize()
    {
        Interlocked.Decrement(ref _queueSizeCount);
    }
}
