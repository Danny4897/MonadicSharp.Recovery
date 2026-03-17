namespace MonadicSharp.Recovery;

/// <summary>
/// Composable predicate factories for selecting which errors enter the Amber track.
/// Designed for use with <see cref="ResultRescueExtensions"/>.
/// </summary>
/// <example>
/// <code>
/// // Simple code match
/// .RescueAsync(ErrorPredicates.HasCode("AI_JSON_INVALID"), recovery)
///
/// // Composed predicates
/// .StartFixBranchAsync(
///     ErrorPredicates.HasAnyCode("AI_TIMEOUT", "AI_RATE_LIMIT")
///                    .Or(ErrorPredicates.IsOfType(ErrorType.Validation)),
///     recovery)
/// </code>
/// </example>
public static class ErrorPredicates
{
    /// <summary>Matches errors with the exact <paramref name="code"/>.</summary>
    public static Func<Error, bool> HasCode(string code) =>
        e => e.HasCode(code);

    /// <summary>Matches errors with any of the provided codes (OR semantics).</summary>
    public static Func<Error, bool> HasAnyCode(params string[] codes) =>
        e => codes.Any(e.HasCode);

    /// <summary>Matches errors of a specific <see cref="ErrorType"/>.</summary>
    public static Func<Error, bool> IsOfType(ErrorType type) =>
        e => e.IsOfType(type);

    /// <summary>Matches every error — rescue unconditionally. Use with care.</summary>
    public static Func<Error, bool> Always() => _ => true;

    /// <summary>Combines two predicates with OR logic.</summary>
    public static Func<Error, bool> Or(
        this Func<Error, bool> left,
        Func<Error, bool> right) =>
        e => left(e) || right(e);

    /// <summary>Combines two predicates with AND logic.</summary>
    public static Func<Error, bool> And(
        this Func<Error, bool> left,
        Func<Error, bool> right) =>
        e => left(e) && right(e);

    /// <summary>Inverts a predicate.</summary>
    public static Func<Error, bool> Not(this Func<Error, bool> predicate) =>
        e => !predicate(e);
}
