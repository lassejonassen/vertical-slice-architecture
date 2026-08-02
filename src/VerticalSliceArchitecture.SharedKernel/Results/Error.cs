namespace VerticalSliceArchitecture.SharedKernel.Results;

/// <summary>
/// An expected failure. Errors are values, not exceptions: they are returned, not thrown.
/// <para>
/// Declare errors in a static catalog next to the aggregate that produces them
/// (see <c>ClientErrors</c>) so codes stay stable and greppable.
/// </para>
/// </summary>
/// <param name="Code">Stable machine-readable identifier, e.g. <c>Client.NotFound</c>.</param>
/// <param name="Description">Human-readable description. Safe to surface to API callers.</param>
/// <param name="Type">Classification used to select a transport status code.</param>
public record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>Sentinel used by successful results. Never surfaced.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public override string ToString() => $"{Code}: {Description}";
}

/// <summary>
/// Aggregates several validation failures into one error so an API caller can fix
/// everything in a single round trip.
/// </summary>
public sealed record ValidationError : Error
{
    public ValidationError(IReadOnlyList<Error> errors)
        : base("General.Validation", "One or more validation errors occurred.", ErrorType.Validation)
    {
        Errors = errors;
    }

    public IReadOnlyList<Error> Errors { get; }

    /// <summary>Collects the errors from every failed result in <paramref name="results"/>.</summary>
    public static ValidationError FromResults(IEnumerable<Result> results) =>
        new([.. results.Where(r => r.IsFailure).Select(r => r.Error)]);
}
