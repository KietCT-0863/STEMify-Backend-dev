using System.Diagnostics.Metrics;

namespace Common.Logging.Metrics;


public static class IdentityMetrics
{
    private static readonly Meter Meter = new("STEMify.Identity", "1.0.0");

    #region Authentication Metrics

    /// <summary>
    /// Total number of login attempts (both successful and failed)
    /// Labels: method (google, local, facebook), status (success, failed)
    /// </summary>
    public static readonly Counter<long> LoginAttempts = Meter.CreateCounter<long>(
        "identity.login.attempts.total",
        unit: "{attempts}",
        description: "Total number of login attempts"
    );

    /// <summary>
    /// Duration of authentication operations
    /// Labels: method, status
    /// </summary>
    public static readonly Histogram<double> AuthenticationDuration = Meter.CreateHistogram<double>(
        "identity.authentication.duration",
        unit: "s",
        description: "Duration of authentication operations in seconds"
    );

    /// <summary>
    /// Total number of token generations (JWT, refresh tokens)
    /// Labels: token_type (access, refresh)
    /// </summary>
    public static readonly Counter<long> TokensGenerated = Meter.CreateCounter<long>(
        "identity.tokens.generated.total",
        unit: "{tokens}",
        description: "Total number of tokens generated"
    );

    /// <summary>
    /// Total number of token validations
    /// Labels: token_type, status (valid, expired, invalid)
    /// </summary>
    public static readonly Counter<long> TokenValidations = Meter.CreateCounter<long>(
        "identity.tokens.validations.total",
        unit: "{validations}",
        description: "Total number of token validations"
    );

    #endregion

    #region User Management Metrics

    /// <summary>
    /// Total number of user registrations
    /// Labels: user_role (student, teacher, admin)
    /// </summary>
    public static readonly Counter<long> UserRegistrations = Meter.CreateCounter<long>(
        "identity.users.registrations.total",
        unit: "{users}",
        description: "Total number of user registrations"
    );

    /// <summary>
    /// Total number of password reset requests
    /// Labels: status (requested, completed, failed)
    /// </summary>
    public static readonly Counter<long> PasswordResetRequests = Meter.CreateCounter<long>(
        "identity.password.reset.requests.total",
        unit: "{requests}",
        description: "Total number of password reset requests"
    );

    /// <summary>
    /// Total number of email verification operations
    /// Labels: status (sent, verified, failed)
    /// </summary>
    public static readonly Counter<long> EmailVerifications = Meter.CreateCounter<long>(
        "identity.email.verifications.total",
        unit: "{verifications}",
        description: "Total number of email verification operations"
    );

    #endregion

    #region Bulk Provisioning Metrics

    /// <summary>
    /// Total number of bulk invitation jobs
    /// Labels: status (created, processing, completed, failed)
    /// </summary>
    public static readonly Counter<long> BulkInvitationJobs = Meter.CreateCounter<long>(
        "identity.bulk.invitation.jobs.total",
        unit: "{jobs}",
        description: "Total number of bulk invitation jobs"
    );

    /// <summary>
    /// Total number of invitations processed from bulk jobs
    /// Labels: status (sent, accepted, rejected, expired)
    /// </summary>
    public static readonly Counter<long> BulkInvitationsProcessed = Meter.CreateCounter<long>(
        "identity.bulk.invitations.processed.total",
        unit: "{invitations}",
        description: "Total number of invitations processed from bulk jobs"
    );

    /// <summary>
    /// Duration of bulk provisioning operations
    /// Labels: operation_type (upload, process, validate)
    /// </summary>
    public static readonly Histogram<double> BulkProvisioningDuration = Meter.CreateHistogram<double>(
        "identity.bulk.provisioning.duration",
        unit: "s",
        description: "Duration of bulk provisioning operations in seconds"
    );

    /// <summary>
    /// Current number of active bulk provisioning jobs
    /// </summary>
    public static readonly ObservableGauge<int> ActiveBulkJobs = Meter.CreateObservableGauge(
        "identity.bulk.jobs.active",
        () => GetActiveBulkJobsCount(),
        unit: "{jobs}",
        description: "Current number of active bulk provisioning jobs"
    );

    private static Func<int> _activeBulkJobsCountProvider = () => 0;

    public static void SetActiveBulkJobsCountProvider(Func<int> provider)
    {
        _activeBulkJobsCountProvider = provider;
    }

    private static int GetActiveBulkJobsCount() => _activeBulkJobsCountProvider();

    #endregion

    #region Security Metrics

    /// <summary>
    /// Total number of security events
    /// Labels: event_type (suspicious_login, account_lockout, password_breach)
    /// </summary>
    public static readonly Counter<long> SecurityEvents = Meter.CreateCounter<long>(
        "identity.security.events.total",
        unit: "{events}",
        description: "Total number of security events"
    );

    /// <summary>
    /// Total number of account lockouts
    /// Labels: reason (failed_attempts, admin_action, security_policy)
    /// </summary>
    public static readonly Counter<long> AccountLockouts = Meter.CreateCounter<long>(
        "identity.accounts.lockouts.total",
        unit: "{lockouts}",
        description: "Total number of account lockouts"
    );

    /// <summary>
    /// Total number of failed login attempts
    /// Labels: method, reason (invalid_credentials, account_locked, account_disabled)
    /// </summary>
    public static readonly Counter<long> FailedLoginAttempts = Meter.CreateCounter<long>(
        "identity.login.failures.total",
        unit: "{attempts}",
        description: "Total number of failed login attempts"
    );

    #endregion

    #region Helper Methods

    public static void RecordLogin(string method, bool success, TimeSpan duration)
    {
        var status = success ? "success" : "failed";
        LoginAttempts.Add(1,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", status));

        AuthenticationDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", status));

        if (!success)
        {
            FailedLoginAttempts.Add(1,
                new KeyValuePair<string, object?>("method", method));
        }
    }

    public static void RecordUserRegistration(string userRole)
    {
        UserRegistrations.Add(1,
            new KeyValuePair<string, object?>("user_role", userRole));
    }

    public static void RecordTokenGeneration(string tokenType)
    {
        TokensGenerated.Add(1,
            new KeyValuePair<string, object?>("token_type", tokenType));
    }

    public static void RecordBulkInvitationJob(string status)
    {
        BulkInvitationJobs.Add(1,
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordBulkInvitationProcessed(string status)
    {
        BulkInvitationsProcessed.Add(1,
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordSecurityEvent(string eventType)
    {
        SecurityEvents.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType));
    }

    public static void RecordEmailVerification(string status)
    {
        EmailVerifications.Add(1,
            new KeyValuePair<string, object?>("status", status));
    }

    public static void RecordBulkProvisioningDuration(string operationType, TimeSpan duration)
    {
        BulkProvisioningDuration.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("operation_type", operationType));
    }

    #endregion
}
