namespace VerticalSliceArchitecture.SharedKernel.Results;

/// <summary>
/// Classifies an <see cref="Error"/> so that transport layers can map it without
/// knowing anything about the specific failure. Add members sparingly: every value
/// here needs a corresponding HTTP status mapping.
/// </summary>
public enum ErrorType
{
    /// <summary>Unexpected failure. Maps to 500.</summary>
    Failure = 0,

    /// <summary>Input violated a rule. Maps to 400.</summary>
    Validation = 1,

    /// <summary>The requested resource does not exist. Maps to 404.</summary>
    NotFound = 2,

    /// <summary>State conflict, e.g. duplicate or illegal transition. Maps to 409.</summary>
    Conflict = 3,

    /// <summary>Caller is not authenticated. Maps to 401.</summary>
    Unauthorized = 4,

    /// <summary>Caller is authenticated but not permitted. Maps to 403.</summary>
    Forbidden = 5
}
