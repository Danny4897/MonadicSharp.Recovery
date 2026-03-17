namespace MonadicSharp.Recovery;

/// <summary>
/// Emitted every time a <see cref="Result{T}"/> enters the Amber (recovery) track.
/// Carry this to any <see cref="IRecoveryTelemetry"/> sink to distinguish
/// auto-resolved errors from critical failures in your observability backend.
/// </summary>
/// <param name="ErrorCode">The error code that triggered recovery.</param>
/// <param name="AttemptNumber">1-based attempt counter within the fix branch.</param>
/// <param name="Succeeded"><c>true</c> if this attempt returned to the Green track.</param>
/// <param name="Duration">Wall-clock time spent on this single recovery attempt.</param>
/// <param name="RecoveryType">"Rescue" (single-attempt) or "FixBranch" (multi-attempt).</param>
public sealed record RecoveryAttemptEvent(
    string   ErrorCode,
    int      AttemptNumber,
    bool     Succeeded,
    TimeSpan Duration,
    string   RecoveryType = "Rescue");
