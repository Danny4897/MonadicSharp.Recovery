namespace MonadicSharp.Recovery;

/// <summary>
/// Observability sink for Amber-track recovery events.
/// Implement this interface to emit <see cref="RecoveryAttemptEvent"/> to
/// OpenTelemetry, Azure App Insights, or any other backend.
/// </summary>
public interface IRecoveryTelemetry
{
    /// <summary>
    /// Record a single recovery attempt on the Amber track.
    /// Called once per attempt, regardless of whether it succeeded.
    /// </summary>
    void RecordAttempt(RecoveryAttemptEvent attempt);
}
