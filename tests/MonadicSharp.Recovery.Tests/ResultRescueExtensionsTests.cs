using FluentAssertions;
using MonadicSharp.Recovery;
using Xunit;

namespace MonadicSharp.Recovery.Tests;

public class RescueAsyncTests
{
    private static readonly Error _targetError =
        Error.Create("Something broke.", "TARGET_CODE");

    private static readonly Error _otherError =
        Error.Create("Other error.", "OTHER_CODE");

    // ── RescueAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RescueAsync_Success_PassesThrough()
    {
        var result = await Task.FromResult(Result<int>.Success(42))
            .RescueAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: _ => Task.FromResult(Result<int>.Success(99)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task RescueAsync_MatchingError_RecoverySucceeds_ReturnsGreenTrack()
    {
        var result = await Task.FromResult(Result<int>.Failure(_targetError))
            .RescueAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: _ => Task.FromResult(Result<int>.Success(99)));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(99);
    }

    [Fact]
    public async Task RescueAsync_MatchingError_RecoveryFails_ReturnsOriginalError()
    {
        var recoveryError = Error.Create("Recovery failed.", "RECOVERY_FAILED");

        var result = await Task.FromResult(Result<int>.Failure(_targetError))
            .RescueAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: _ => Task.FromResult(Result<int>.Failure(recoveryError)));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TARGET_CODE");  // original, not recovery error
    }

    [Fact]
    public async Task RescueAsync_NonMatchingError_PassesThrough()
    {
        var result = await Task.FromResult(Result<int>.Failure(_otherError))
            .RescueAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: _ => Task.FromResult(Result<int>.Success(99)));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OTHER_CODE");
    }

    [Fact]
    public async Task RescueAsync_RecoveryThrows_ReturnsOriginalError()
    {
        var result = await Task.FromResult(Result<int>.Failure(_targetError))
            .RescueAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: _ => throw new InvalidOperationException("boom"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TARGET_CODE");
    }

    [Fact]
    public async Task RescueAsync_EmitsTelemetry_OnAttempt()
    {
        var sink = new CaptureTelemetry();

        await Task.FromResult(Result<int>.Failure(_targetError))
            .RescueAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: _ => Task.FromResult(Result<int>.Success(1)),
                telemetry: sink);

        sink.Events.Should().ContainSingle();
        sink.Events[0].ErrorCode.Should().Be("TARGET_CODE");
        sink.Events[0].Succeeded.Should().BeTrue();
        sink.Events[0].RecoveryType.Should().Be("Rescue");
    }

    // ── StartFixBranchAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task FixBranch_SucceedsOnFirstAttempt_ReturnsGreenTrack()
    {
        var result = await Task.FromResult(Result<int>.Failure(_targetError))
            .StartFixBranchAsync(
                when:     ErrorPredicates.HasCode("TARGET_CODE"),
                recovery: (_, _) => Task.FromResult(Result<int>.Success(1)),
                maxAttempts: 3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task FixBranch_FailsAllAttempts_ReturnsOriginalError()
    {
        int callCount = 0;

        var result = await Task.FromResult(Result<int>.Failure(_targetError))
            .StartFixBranchAsync(
                when:        ErrorPredicates.HasCode("TARGET_CODE"),
                recovery:    (_, _) => { callCount++; return Task.FromResult(Result<int>.Failure(Error.Create("repair fail"))); },
                maxAttempts: 3);

        callCount.Should().Be(3);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TARGET_CODE"); // original
    }

    [Fact]
    public async Task FixBranch_SucceedsOnSecondAttempt_StopsEarly()
    {
        int callCount = 0;

        var result = await Task.FromResult(Result<int>.Failure(_targetError))
            .StartFixBranchAsync(
                when:        ErrorPredicates.HasCode("TARGET_CODE"),
                recovery:    (_, attempt) =>
                {
                    callCount++;
                    return attempt == 2
                        ? Task.FromResult(Result<int>.Success(42))
                        : Task.FromResult(Result<int>.Failure(Error.Create("not yet")));
                },
                maxAttempts: 3);

        callCount.Should().Be(2); // stopped after success on attempt 2
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task FixBranch_EmitsTelemetryPerAttempt()
    {
        var sink = new CaptureTelemetry();
        int callCount = 0;

        await Task.FromResult(Result<int>.Failure(_targetError))
            .StartFixBranchAsync(
                when:        ErrorPredicates.HasCode("TARGET_CODE"),
                recovery:    (_, _) => { callCount++; return Task.FromResult(Result<int>.Failure(Error.Create("fail"))); },
                maxAttempts: 2,
                telemetry:   sink);

        sink.Events.Should().HaveCount(2);
        sink.Events.Should().AllSatisfy(e => e.RecoveryType.Should().Be("FixBranch"));
        sink.Events.Should().AllSatisfy(e => e.Succeeded.Should().BeFalse());
    }

    // ── ErrorPredicates ──────────────────────────────────────────────────────

    [Fact]
    public void ErrorPredicates_Or_Composes()
    {
        var pred = ErrorPredicates.HasCode("A").Or(ErrorPredicates.HasCode("B"));

        pred(Error.Create("msg", "A")).Should().BeTrue();
        pred(Error.Create("msg", "B")).Should().BeTrue();
        pred(Error.Create("msg", "C")).Should().BeFalse();
    }

    [Fact]
    public void ErrorPredicates_Not_Inverts()
    {
        var pred = ErrorPredicates.HasCode("A").Not();

        pred(Error.Create("msg", "A")).Should().BeFalse();
        pred(Error.Create("msg", "B")).Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private sealed class CaptureTelemetry : IRecoveryTelemetry
    {
        public List<RecoveryAttemptEvent> Events { get; } = new();
        public void RecordAttempt(RecoveryAttemptEvent attempt) => Events.Add(attempt);
    }
}
