using System.Diagnostics.Metrics;

namespace Common.Logging.Metrics;

/// <summary>
/// Metrics for Order Service - Order lifecycle and Saga orchestration
/// Following OpenTelemetry semantic conventions and best practices
/// </summary>
public static class OrderMetrics
{
    private static readonly Meter Meter = new("STEMify.Order", "1.0.0");

    #region Order Lifecycle Metrics

    /// <summary>
    /// Total number of orders created
    /// Labels: order_type (product, course, subscription)
    /// </summary>
    public static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>(
        "order.orders.created.total",
        unit: "{orders}",
        description: "Total number of orders created"
    );

    /// <summary>
    /// Total number of orders completed
    /// Labels: order_type, completion_status (success, partial)
    /// </summary>
    public static readonly Counter<long> OrdersCompleted = Meter.CreateCounter<long>(
        "order.orders.completed.total",
        unit: "{orders}",
        description: "Total number of orders completed"
    );

    /// <summary>
    /// Total number of orders cancelled
    /// Labels: order_type, cancellation_reason (user_request, timeout, payment_failed, stock_unavailable)
    /// </summary>
    public static readonly Counter<long> OrdersCancelled = Meter.CreateCounter<long>(
        "order.orders.cancelled.total",
        unit: "{orders}",
        description: "Total number of orders cancelled"
    );

    /// <summary>
    /// Duration of order processing from creation to completion
    /// Labels: order_type, status
    /// </summary>
    public static readonly Histogram<double> OrderProcessingDuration = Meter.CreateHistogram<double>(
        "order.processing.duration",
        unit: "s",
        description: "Duration of order processing in seconds"
    );

    /// <summary>
    /// Current number of pending orders
    /// </summary>
    public static readonly ObservableGauge<int> PendingOrders = Meter.CreateObservableGauge(
        "order.orders.pending",
        () => GetPendingOrdersCount(),
        unit: "{orders}",
        description: "Current number of pending orders"
    );

    private static Func<int> _pendingOrdersCountProvider = () => 0;

    public static void SetPendingOrdersCountProvider(Func<int> provider)
    {
        _pendingOrdersCountProvider = provider;
    }

    private static int GetPendingOrdersCount() => _pendingOrdersCountProvider();

    #endregion

    #region Order Items Metrics

    /// <summary>
    /// Total number of order items
    /// Labels: product_type (course, physical_product, digital_product)
    /// </summary>
    public static readonly Counter<long> OrderItems = Meter.CreateCounter<long>(
        "order.items.total",
        unit: "{items}",
        description: "Total number of order items"
    );

    /// <summary>
    /// Total order value
    /// Labels: currency (VND, USD)
    /// Note: Using counter for cumulative value tracking
    /// </summary>
    public static readonly Counter<decimal> OrderValue = Meter.CreateCounter<decimal>(
        "order.value.total",
        unit: "VND",
        description: "Total order value in VND"
    );

    #endregion

    #region Saga Orchestration Metrics

    /// <summary>
    /// Total number of saga transactions initiated
    /// Labels: saga_type (order_creation, order_cancellation)
    /// </summary>
    public static readonly Counter<long> SagaTransactions = Meter.CreateCounter<long>(
        "order.saga.transactions.total",
        unit: "{transactions}",
        description: "Total number of saga transactions"
    );

    /// <summary>
    /// Total number of saga compensations executed
    /// Labels: saga_type, compensation_reason
    /// </summary>
    public static readonly Counter<long> SagaCompensations = Meter.CreateCounter<long>(
        "order.saga.compensations.total",
        unit: "{compensations}",
        description: "Total number of saga compensations executed"
    );

    /// <summary>
    /// Duration of saga transactions
    /// Labels: saga_type, status (completed, compensated, failed)
    /// </summary>
    public static readonly Histogram<double> SagaDuration = Meter.CreateHistogram<double>(
        "order.saga.duration",
        unit: "s",
        description: "Duration of saga transactions in seconds"
    );

    /// <summary>
    /// Current number of active saga transactions
    /// </summary>
    public static readonly ObservableGauge<int> ActiveSagas = Meter.CreateObservableGauge(
        "order.saga.active",
        () => GetActiveSagasCount(),
        unit: "{sagas}",
        description: "Current number of active saga transactions"
    );

    private static Func<int> _activeSagasCountProvider = () => 0;

    public static void SetActiveSagasCountProvider(Func<int> provider)
    {
        _activeSagasCountProvider = provider;
    }

    private static int GetActiveSagasCount() => _activeSagasCountProvider();

    #endregion

    #region Stock Management Metrics

    /// <summary>
    /// Total number of stock reservations
    /// Labels: status (reserved, released, expired)
    /// </summary>
    public static readonly Counter<long> StockReservations = Meter.CreateCounter<long>(
        "order.stock.reservations.total",
        unit: "{reservations}",
        description: "Total number of stock reservations"
    );

    /// <summary>
    /// Total number of stock-out events
    /// Labels: product_type
    /// </summary>
    public static readonly Counter<long> StockOutEvents = Meter.CreateCounter<long>(
        "order.stock.out.events.total",
        unit: "{events}",
        description: "Total number of stock-out events"
    );

    #endregion

    #region Helper Methods

    public static void RecordOrderCreated(string orderType, decimal orderValue)
    {
        OrdersCreated.Add(1,
            new KeyValuePair<string, object?>("order_type", orderType));

        OrderValue.Add(orderValue,
            new KeyValuePair<string, object?>("currency", "VND"));
    }

    public static void RecordOrderCompleted(string orderType, TimeSpan duration)
    {
        OrdersCompleted.Add(1,
            new KeyValuePair<string, object?>("order_type", orderType));

        OrderProcessingDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("order_type", orderType),
            new KeyValuePair<string, object?>("status", "completed"));
    }

    public static void RecordOrderCancelled(string orderType, string cancellationReason)
    {
        OrdersCancelled.Add(1,
            new KeyValuePair<string, object?>("order_type", orderType),
            new KeyValuePair<string, object?>("cancellation_reason", cancellationReason));
    }

    public static void RecordSagaTransaction(string sagaType, string status, TimeSpan duration)
    {
        SagaTransactions.Add(1,
            new KeyValuePair<string, object?>("saga_type", sagaType));

        SagaDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("saga_type", sagaType),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordSagaCompensation(string sagaType, string reason)
    {
        SagaCompensations.Add(1,
            new KeyValuePair<string, object?>("saga_type", sagaType),
            new KeyValuePair<string, object?>("compensation_reason", reason));
    }

    #endregion
}
