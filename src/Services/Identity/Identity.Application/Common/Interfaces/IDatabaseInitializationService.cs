namespace Identity.Application.Common.Interfaces;

/// <summary>
/// Interface for database initialization status tracking
/// Following Clean Architecture - interfaces belong in Application layer
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Gets whether the database has been successfully initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the initialization exception if initialization failed
    /// </summary>
    Exception? InitializationException { get; }

    /// <summary>
    /// Marks the database as successfully initialized
    /// </summary>
    void MarkAsInitialized();

    /// <summary>
    /// Marks the database initialization as failed with the given exception
    /// </summary>
    /// <param name="exception">The exception that caused initialization to fail</param>
    void MarkAsFailed(Exception exception);

    Task WaitUntilInitializedAsync(CancellationToken cancellationToken = default);
}
