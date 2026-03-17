using System.Diagnostics;

namespace MonadicSharp.Recovery;

/// <summary>
/// Amber-track (self-healing) extension methods for <see cref="Result{T}"/>.
/// </summary>
/// <remarks>
/// Three-track railway model:
/// <code>
///   GREEN  ──────────────────────────────────────────────────►  Success
///                    │ error matches predicate
///                    ▼
///   AMBER  ──── RescueAsync / StartFixBranchAsync ──────────►  merged → GREEN
///                    │ all attempts fail
///                    ▼
///   RED    ──── original error propagated ─────────────────►  Failure
/// </code>
/// The original error is always preserved on the Red track.
/// Recovery errors are never surfaced to the caller — only the root cause is.
/// </remarks>
public static class ResultRescueExtensions
{
    // ── RescueAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// Intercepts a failure matching <paramref name="when"/> and attempts a single recovery.
    /// Returns to the Green track on success; propagates the <b>original</b> error on failure.
    /// Errors that do not match <paramref name="when"/> pass through unchanged.
    /// </summary>
    /// <param name="resultTask">The failing pipeline result to rescue.</param>
    /// <param name="when">Predicate selecting which errors enter the Amber track.</param>
    /// <param name="recovery">Repair function — receives the original error, returns a new result.</param>
    /// <param name="telemetry">Optional sink for <see cref="RecoveryAttemptEvent"/>.</param>
    public static async Task<Result<T>> RescueAsync<T>(
        this Task<Result<T>>         resultTask,
        Func<Error, bool>            when,
        Func<Error, Task<Result<T>>> recovery,
        IRecoveryTelemetry?          telemetry = null)
    {
        var result = await resultTask.ConfigureAwait(false);

        if (result.IsSuccess || !when(result.Error!))
            return result;

        var originalError = result.Error!;
        var sw = Stopwatch.StartNew();
        Result<T> recovered;

        try
        {
            recovered = await recovery(originalError).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            recovered = Result<T>.Failure(
                Error.FromException(ex, "RESCUE_EXCEPTION").WithInnerError(originalError));
        }
        finally { sw.Stop(); }

        telemetry?.RecordAttempt(new RecoveryAttemptEvent(
            ErrorCode:     originalError.Code ?? "UNKNOWN",
            AttemptNumber: 1,
            Succeeded:     recovered.IsSuccess,
            Duration:      sw.Elapsed,
            RecoveryType:  "Rescue"));

        // Preserve original error on Red track — caller sees root cause, not the repair failure.
        return recovered.IsSuccess
            ? recovered
            : Result<T>.Failure(originalError);
    }

    /// <summary>Sync-result overload of <see cref="RescueAsync{T}(Task{Result{T}},Func{Error,bool},Func{Error,Task{Result{T}}},IRecoveryTelemetry?)"/>.</summary>
    public static Task<Result<T>> RescueAsync<T>(
        this Result<T>               result,
        Func<Error, bool>            when,
        Func<Error, Task<Result<T>>> recovery,
        IRecoveryTelemetry?          telemetry = null) =>
        Task.FromResult(result).RescueAsync(when, recovery, telemetry);

    // ── StartFixBranchAsync ──────────────────────────────────────────────────

    /// <summary>
    /// Opens a fix branch on a matched failure and retries recovery up to
    /// <paramref name="maxAttempts"/> times with optional backoff between attempts.
    /// </summary>
    /// <remarks>
    /// Modelled on a GitHub fix branch:
    /// <list type="bullet">
    ///   <item>Branch is opened on the first matching failure.</item>
    ///   <item>Each attempt receives the original error and its attempt number (1-based).</item>
    ///   <item>Use the attempt number to escalate strategy (e.g. different model, simpler prompt).</item>
    ///   <item>First success → branch "merged" back to Green track.</item>
    ///   <item>All attempts fail → branch "closed", original error propagated to Red track.</item>
    /// </list>
    /// </remarks>
    /// <param name="resultTask">The failing pipeline result to fix.</param>
    /// <param name="when">Predicate selecting which errors open the fix branch.</param>
    /// <param name="recovery">
    ///   Repair function: (originalError, attemptNumber) → Result.
    ///   Use attemptNumber to escalate strategy across retries.
    /// </param>
    /// <param name="maxAttempts">Maximum repair attempts before giving up (default 3).</param>
    /// <param name="delayBetweenAttempts">Optional delay between attempts (null = no delay).</param>
    /// <param name="telemetry">Optional sink for <see cref="RecoveryAttemptEvent"/>.</param>
    public static async Task<Result<T>> StartFixBranchAsync<T>(
        this Task<Result<T>>                resultTask,
        Func<Error, bool>                   when,
        Func<Error, int, Task<Result<T>>>   recovery,
        int                                 maxAttempts          = 3,
        TimeSpan?                           delayBetweenAttempts = null,
        IRecoveryTelemetry?                 telemetry            = null)
    {
        var result = await resultTask.ConfigureAwait(false);

        if (result.IsSuccess || !when(result.Error!))
            return result;

        var originalError = result.Error!;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (attempt > 1 && delayBetweenAttempts.HasValue)
                await Task.Delay(delayBetweenAttempts.Value).ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            Result<T> recovered;

            try
            {
                recovered = await recovery(originalError, attempt).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                recovered = Result<T>.Failure(
                    Error.FromException(ex, "FIX_BRANCH_EXCEPTION").WithInnerError(originalError));
            }
            finally { sw.Stop(); }

            telemetry?.RecordAttempt(new RecoveryAttemptEvent(
                ErrorCode:     originalError.Code ?? "UNKNOWN",
                AttemptNumber: attempt,
                Succeeded:     recovered.IsSuccess,
                Duration:      sw.Elapsed,
                RecoveryType:  "FixBranch"));

            if (recovered.IsSuccess)
                return recovered; // ✅ merged back to Green
        }

        // All fix attempts exhausted — Red track with the original root cause.
        return Result<T>.Failure(originalError);
    }

    /// <summary>Sync-result overload of <see cref="StartFixBranchAsync{T}(Task{Result{T}},Func{Error,bool},Func{Error,int,Task{Result{T}}},int,TimeSpan?,IRecoveryTelemetry?)"/>.</summary>
    public static Task<Result<T>> StartFixBranchAsync<T>(
        this Result<T>                     result,
        Func<Error, bool>                  when,
        Func<Error, int, Task<Result<T>>>  recovery,
        int                                maxAttempts          = 3,
        TimeSpan?                          delayBetweenAttempts = null,
        IRecoveryTelemetry?                telemetry            = null) =>
        Task.FromResult(result).StartFixBranchAsync(when, recovery, maxAttempts, delayBetweenAttempts, telemetry);
}
