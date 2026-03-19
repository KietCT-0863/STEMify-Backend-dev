using System.Diagnostics.Metrics;

namespace Common.Logging.Metrics;

/// <summary>
/// Metrics for Payment Service - Payment processing and gateway integration
/// Following OpenTelemetry semantic conventions and best practices
/// </summary>
public static class PaymentMetrics
{
    private static readonly Meter Meter = new("STEMify.Payment", "1.0.0");

    #region Payment Processing Metrics

    /// <summary>
    /// Total number of payment transactions
    /// Labels: payment_method (vnpay, momo, paypal, stripe), status (success, failed, pending)
    /// </summary>
    public static readonly Counter<long> PaymentTransactions = Meter.CreateCounter<long>(
        "payment.transactions.total",
        unit: "{transactions}",
        description: "Total number of payment transactions"
    );

    /// <summary>
    /// Total payment amount processed
    /// Labels: payment_method, currency, status
    /// </summary>
    public static readonly Counter<decimal> PaymentAmount = Meter.CreateCounter<decimal>(
        "payment.amount.total",
        unit: "VND",
        description: "Total payment amount processed in VND"
    );

    /// <summary>
    /// Duration of payment processing
    /// Labels: payment_method, status
    /// </summary>
    public static readonly Histogram<double> PaymentDuration = Meter.CreateHistogram<double>(
        "payment.processing.duration",
        unit: "s",
        description: "Duration of payment processing in seconds"
    );

    /// <summary>
    /// Total number of payment refunds
    /// Labels: payment_method, refund_type (full, partial)
    /// </summary>
    public static readonly Counter<long> PaymentRefunds = Meter.CreateCounter<long>(
        "payment.refunds.total",
        unit: "{refunds}",
        description: "Total number of payment refunds"
    );

    /// <summary>
    /// Total refund amount
    /// Labels: payment_method, currency
    /// </summary>
    public static readonly Counter<decimal> RefundAmount = Meter.CreateCounter<decimal>(
        "payment.refund.amount.total",
        unit: "VND",
        description: "Total refund amount in VND"
    );

    #endregion

    #region Payment Gateway Metrics

    /// <summary>
    /// Total number of gateway API calls
    /// Labels: gateway (vnpay, momo, paypal), operation (create, verify, refund)
    /// </summary>
    public static readonly Counter<long> GatewayApiCalls = Meter.CreateCounter<long>(
        "payment.gateway.api.calls.total",
        unit: "{calls}",
        description: "Total number of payment gateway API calls"
    );

    /// <summary>
    /// Duration of gateway API calls
    /// Labels: gateway, operation, status
    /// </summary>
    public static readonly Histogram<double> GatewayApiDuration = Meter.CreateHistogram<double>(
        "payment.gateway.api.duration",
        unit: "s",
        description: "Duration of payment gateway API calls in seconds"
    );

    /// <summary>
    /// Total number of gateway errors
    /// Labels: gateway, error_type (timeout, invalid_response, network_error)
    /// </summary>
    public static readonly Counter<long> GatewayErrors = Meter.CreateCounter<long>(
        "payment.gateway.errors.total",
        unit: "{errors}",
        description: "Total number of payment gateway errors"
    );

    /// <summary>
    /// Total number of gateway callbacks received
    /// Labels: gateway, callback_type (ipn, return_url)
    /// </summary>
    public static readonly Counter<long> GatewayCallbacks = Meter.CreateCounter<long>(
        "payment.gateway.callbacks.total",
        unit: "{callbacks}",
        description: "Total number of gateway callbacks received"
    );

    #endregion

    #region Payment Validation Metrics

    /// <summary>
    /// Total number of payment signature validations
    /// Labels: gateway, status (valid, invalid)
    /// </summary>
    public static readonly Counter<long> SignatureValidations = Meter.CreateCounter<long>(
        "payment.signature.validations.total",
        unit: "{validations}",
        description: "Total number of payment signature validations"
    );

    /// <summary>
    /// Total number of suspicious payment attempts
    /// Labels: reason (signature_mismatch, duplicate_transaction, amount_mismatch)
    /// </summary>
    public static readonly Counter<long> SuspiciousPayments = Meter.CreateCounter<long>(
        "payment.suspicious.attempts.total",
        unit: "{attempts}",
        description: "Total number of suspicious payment attempts"
    );

    #endregion

    #region Payment Status Metrics

    /// <summary>
    /// Current number of pending payments
    /// </summary>
    public static readonly ObservableGauge<int> PendingPayments = Meter.CreateObservableGauge(
        "payment.pending",
        () => GetPendingPaymentsCount(),
        unit: "{payments}",
        description: "Current number of pending payments"
    );

    /// <summary>
    /// Total number of payment timeouts
    /// Labels: payment_method, timeout_stage (gateway, confirmation)
    /// </summary>
    public static readonly Counter<long> PaymentTimeouts = Meter.CreateCounter<long>(
        "payment.timeouts.total",
        unit: "{timeouts}",
        description: "Total number of payment timeouts"
    );

    private static Func<int> _pendingPaymentsCountProvider = () => 0;

    public static void SetPendingPaymentsCountProvider(Func<int> provider)
    {
        _pendingPaymentsCountProvider = provider;
    }

    private static int GetPendingPaymentsCount() => _pendingPaymentsCountProvider();

    #endregion

    #region Helper Methods

    public static void RecordPaymentTransaction(string paymentMethod, string status, decimal amount, TimeSpan duration)
    {
        PaymentTransactions.Add(1,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("status", status));

        PaymentAmount.Add(amount,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("currency", "VND"),
            new KeyValuePair<string, object?>("status", status));

        PaymentDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordGatewayApiCall(string gateway, string operation, string status, TimeSpan duration)
    {
        GatewayApiCalls.Add(1,
            new KeyValuePair<string, object?>("gateway", gateway),
            new KeyValuePair<string, object?>("operation", operation));

        GatewayApiDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("gateway", gateway),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordGatewayError(string gateway, string errorType)
    {
        GatewayErrors.Add(1,
            new KeyValuePair<string, object?>("gateway", gateway),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    public static void RecordRefund(string paymentMethod, string refundType, decimal refundAmount)
    {
        PaymentRefunds.Add(1,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("refund_type", refundType));

        RefundAmount.Add(refundAmount,
            new KeyValuePair<string, object?>("payment_method", paymentMethod),
            new KeyValuePair<string, object?>("currency", "VND"));
    }

    public static void RecordSuspiciousPayment(string reason)
    {
        SuspiciousPayments.Add(1,
            new KeyValuePair<string, object?>("reason", reason));
    }

    #endregion
}
