# MonadicSharp.Recovery

Amber-track (self-healing) extension for [MonadicSharp](https://github.com/Danny4897/MonadicSharp).

Adds `RescueAsync` and `StartFixBranchAsync` to your `Result<T>` pipelines — intercept specific failures, attempt recovery, and merge back to the success track without breaking the Railway-Oriented Programming flow.

```
GREEN  ──── Bind → Bind → Map ─────────────────────────────► Success
                 │ error matches predicate
                 ▼
AMBER  ──── RescueAsync / StartFixBranchAsync ──────────────► merged → GREEN
                 │ all attempts fail
                 ▼
RED    ──── original error preserved ───────────────────────► Failure
```

## Install

```bash
dotnet add package MonadicSharp.Recovery
```

## Quick Start

```csharp
using MonadicSharp.Recovery;

// Single recovery attempt (RescueAsync)
var result = await CallExternalApiAsync()
    .RescueAsync(
        when:     ErrorPredicates.HasCode("TIMEOUT"),
        recovery: _ => CallFallbackApiAsync());

// Multi-attempt fix branch with backoff (StartFixBranchAsync)
var result = await GenerateWithLlmAsync()
    .StartFixBranchAsync(
        when:                 ErrorPredicates.HasAnyCode("AI_JSON_INVALID", "AI_TIMEOUT"),
        recovery:             (error, attempt) => RetryWithDifferentStrategyAsync(error, attempt),
        maxAttempts:          3,
        delayBetweenAttempts: TimeSpan.FromSeconds(1));
```

## Core Operators

### `RescueAsync` — single recovery attempt

Intercepts an error matching the predicate, runs a recovery function once.
- Recovery succeeds → back to Green track
- Recovery fails → **original** error propagated to Red (root cause preserved)

```csharp
await pipeline
    .RescueAsync(
        when:     ErrorPredicates.HasCode("DB_TIMEOUT"),
        recovery: error => FallbackRepositoryAsync(error));
```

### `StartFixBranchAsync` — N-attempt fix branch

Opens a "fix branch" on a matched failure. Retry up to N times with optional backoff.
The recovery function receives the attempt number so you can escalate strategy.

```csharp
await pipeline
    .StartFixBranchAsync(
        when: ErrorPredicates.HasAnyCode("AI_JSON_INVALID", "AI_SCHEMA_MISMATCH"),
        recovery: (error, attempt) => attempt switch
        {
            1 => RepairWithSameModelAsync(error),
            _ => RepairWithFallbackModelAsync(error),   // escalate on attempt 2+
        },
        maxAttempts:          3,
        delayBetweenAttempts: TimeSpan.FromMilliseconds(500));
```

## ErrorPredicates

Composable, combinable filters for selecting which errors enter the Amber track:

```csharp
// Single code
ErrorPredicates.HasCode("AI_JSON_INVALID")

// Multiple codes (OR)
ErrorPredicates.HasAnyCode("AI_TIMEOUT", "AI_RATE_LIMIT")

// By error type
ErrorPredicates.IsOfType(ErrorType.Validation)

// Composition
ErrorPredicates.HasCode("AI_TIMEOUT")
               .Or(ErrorPredicates.HasCode("AI_RATE_LIMIT"))
               .And(ErrorPredicates.IsOfType(ErrorType.Validation).Not())
```

## Observability — IRecoveryTelemetry

Implement `IRecoveryTelemetry` to emit events to any backend:

```csharp
public class MyTelemetry : IRecoveryTelemetry
{
    public void RecordAttempt(RecoveryAttemptEvent attempt)
    {
        // attempt.ErrorCode, attempt.AttemptNumber, attempt.Succeeded, attempt.Duration
        // emit to OpenTelemetry, App Insights, Datadog, etc.
    }
}
```

Pass the sink to any operator:
```csharp
.RescueAsync(when, recovery, telemetry: new MyTelemetry())
.StartFixBranchAsync(when, recovery, telemetry: new MyTelemetry())
```

### Reading the three-lane highway in logs

Use structured logging prefixes to filter in App Insights / Grafana:

| Log prefix | Meaning |
|---|---|
| `[Amber→Green]` | Error auto-healed — back on success track |
| `[Amber→Red]` | Recovery failed — original error propagated |

```kusto
// Azure App Insights: recovery rate by error code
customEvents
| where name == "RecoveryAttempt"
| extend code      = tostring(customDimensions["agentscope.recovery.error_code"])
| extend succeeded = tobool(customDimensions["agentscope.recovery.succeeded"])
| summarize total = count(), healed = countif(succeeded) by code
| extend heal_rate = round(100.0 * healed / total, 1)
```

## Combining with MonadicSharp.AI

`MonadicSharp.Recovery` and `MonadicSharp.AI` are independent packages that compose naturally:

```csharp
using MonadicSharp.Recovery;
using MonadicSharp.AI.Errors;      // AiError codes
using MonadicSharp.AI.Extensions;  // WithSelfHealingAsync

// Use AiError codes as predicates for Recovery operators
await pipeline
    .StartFixBranchAsync(
        when:     ErrorPredicates.HasCode(AiError.InvalidOutputCode),
        recovery: (error, attempt) => RepairOutputAsync(error, attempt));
```

## License

MIT
