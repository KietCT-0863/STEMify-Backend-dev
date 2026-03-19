using System.Diagnostics.Metrics;

namespace Common.Logging.Metrics;

/// <summary>
/// Metrics for RabbitMQ message processing
/// Following OpenTelemetry semantic conventions and best practices
/// </summary>
public static class RabbitMqMetrics
{
    private static readonly Meter Meter = new("STEMify.RabbitMQ", "1.0.0");

    #region Message Publishing Metrics

    /// <summary>
    /// Total number of messages published
    /// Labels: exchange, routing_key, message_type
    /// </summary>
    public static readonly Counter<long> MessagesPublished = Meter.CreateCounter<long>(
        "rabbitmq.messages.published.total",
        unit: "{messages}",
        description: "Total number of messages published to RabbitMQ"
    );

    /// <summary>
    /// Duration of message publishing
    /// Labels: exchange, routing_key
    /// </summary>
    public static readonly Histogram<double> PublishDuration = Meter.CreateHistogram<double>(
        "rabbitmq.publish.duration",
        unit: "s",
        description: "Duration of message publishing in seconds"
    );

    /// <summary>
    /// Total number of publishing failures
    /// Labels: exchange, routing_key, error_type
    /// </summary>
    public static readonly Counter<long> PublishFailures = Meter.CreateCounter<long>(
        "rabbitmq.publish.failures.total",
        unit: "{failures}",
        description: "Total number of message publishing failures"
    );

    #endregion

    #region Message Consumption Metrics

    /// <summary>
    /// Total number of messages consumed
    /// Labels: queue, message_type, status (ack, nack, reject)
    /// </summary>
    public static readonly Counter<long> MessagesConsumed = Meter.CreateCounter<long>(
        "rabbitmq.messages.consumed.total",
        unit: "{messages}",
        description: "Total number of messages consumed from RabbitMQ"
    );

    /// <summary>
    /// Duration of message processing
    /// Labels: queue, message_type, status
    /// </summary>
    public static readonly Histogram<double> ProcessingDuration = Meter.CreateHistogram<double>(
        "rabbitmq.processing.duration",
        unit: "s",
        description: "Duration of message processing in seconds"
    );

    /// <summary>
    /// Total number of message processing retries
    /// Labels: queue, message_type, retry_count
    /// </summary>
    public static readonly Counter<long> ProcessingRetries = Meter.CreateCounter<long>(
        "rabbitmq.processing.retries.total",
        unit: "{retries}",
        description: "Total number of message processing retries"
    );

    /// <summary>
    /// Total number of messages moved to dead letter queue
    /// Labels: queue, message_type, reason
    /// </summary>
    public static readonly Counter<long> DeadLetterMessages = Meter.CreateCounter<long>(
        "rabbitmq.deadletter.messages.total",
        unit: "{messages}",
        description: "Total number of messages moved to dead letter queue"
    );

    #endregion

    #region Queue Metrics

    /// <summary>
    /// Current number of messages in queue
    /// Labels: queue
    /// </summary>
    public static readonly ObservableGauge<int> QueueDepth = Meter.CreateObservableGauge(
        "rabbitmq.queue.depth",
        () => GetQueueDepth(),
        unit: "{messages}",
        description: "Current number of messages in queue"
    );

    /// <summary>
    /// Current number of consumers
    /// Labels: queue
    /// </summary>
    public static readonly ObservableGauge<int> ConsumerCount = Meter.CreateObservableGauge(
        "rabbitmq.consumers.count",
        () => GetConsumerCount(),
        unit: "{consumers}",
        description: "Current number of active consumers"
    );

    /// <summary>
    /// Message age in queue (oldest message)
    /// Labels: queue
    /// </summary>
    public static readonly ObservableGauge<double> MessageAge = Meter.CreateObservableGauge(
        "rabbitmq.message.age",
        () => GetMessageAge(),
        unit: "s",
        description: "Age of oldest message in queue in seconds"
    );

    private static Func<int> _queueDepthProvider = () => 0;
    private static Func<int> _consumerCountProvider = () => 0;
    private static Func<double> _messageAgeProvider = () => 0;

    public static void SetQueueDepthProvider(Func<int> provider)
    {
        _queueDepthProvider = provider;
    }

    public static void SetConsumerCountProvider(Func<int> provider)
    {
        _consumerCountProvider = provider;
    }

    public static void SetMessageAgeProvider(Func<double> provider)
    {
        _messageAgeProvider = provider;
    }

    private static int GetQueueDepth() => _queueDepthProvider();
    private static int GetConsumerCount() => _consumerCountProvider();
    private static double GetMessageAge() => _messageAgeProvider();

    #endregion

    #region Connection Metrics

    /// <summary>
    /// Total number of connection events
    /// Labels: event_type (connected, disconnected, reconnected)
    /// </summary>
    public static readonly Counter<long> ConnectionEvents = Meter.CreateCounter<long>(
        "rabbitmq.connection.events.total",
        unit: "{events}",
        description: "Total number of connection events"
    );

    /// <summary>
    /// Current connection status
    /// 1 = connected, 0 = disconnected
    /// </summary>
    public static readonly ObservableGauge<int> ConnectionStatus = Meter.CreateObservableGauge(
        "rabbitmq.connection.status",
        () => GetConnectionStatus(),
        unit: "1",
        description: "Current connection status (1=connected, 0=disconnected)"
    );

    private static Func<int> _connectionStatusProvider = () => 0;

    public static void SetConnectionStatusProvider(Func<int> provider)
    {
        _connectionStatusProvider = provider;
    }

    private static int GetConnectionStatus() => _connectionStatusProvider();

    #endregion

    #region Helper Methods

    public static void RecordMessagePublished(string exchange, string routingKey, string messageType, TimeSpan duration)
    {
        MessagesPublished.Add(1,
            new KeyValuePair<string, object?>("exchange", exchange),
            new KeyValuePair<string, object?>("routing_key", routingKey),
            new KeyValuePair<string, object?>("message_type", messageType));

        PublishDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("exchange", exchange),
            new KeyValuePair<string, object?>("routing_key", routingKey));
    }

    public static void RecordPublishFailure(string exchange, string routingKey, string errorType)
    {
        PublishFailures.Add(1,
            new KeyValuePair<string, object?>("exchange", exchange),
            new KeyValuePair<string, object?>("routing_key", routingKey),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    public static void RecordMessageConsumed(string queue, string messageType, string status, TimeSpan duration)
    {
        MessagesConsumed.Add(1,
            new KeyValuePair<string, object?>("queue", queue),
            new KeyValuePair<string, object?>("message_type", messageType),
            new KeyValuePair<string, object?>("status", status));

        ProcessingDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("queue", queue),
            new KeyValuePair<string, object?>("message_type", messageType),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordProcessingRetry(string queue, string messageType, int retryCount)
    {
        ProcessingRetries.Add(1,
            new KeyValuePair<string, object?>("queue", queue),
            new KeyValuePair<string, object?>("message_type", messageType),
            new KeyValuePair<string, object?>("retry_count", retryCount.ToString()));
    }

    public static void RecordDeadLetterMessage(string queue, string messageType, string reason)
    {
        DeadLetterMessages.Add(1,
            new KeyValuePair<string, object?>("queue", queue),
            new KeyValuePair<string, object?>("message_type", messageType),
            new KeyValuePair<string, object?>("reason", reason));
    }

    public static void RecordConnectionEvent(string eventType)
    {
        ConnectionEvents.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType));
    }

    #endregion
}
