using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Clients;

/// <summary>
/// Every failure the <see cref="Client"/> aggregate can produce, in one place.
/// Keeping them here rather than inline makes the codes greppable, stable across refactors,
/// and directly assertable in tests.
/// </summary>
public static class ClientErrors
{
    public static readonly Error CompanyNameEmpty =
        Error.Validation("Client.CompanyName.Empty", "Company name is required.");

    public static readonly Error CompanyNameTooLong =
        Error.Validation(
            "Client.CompanyName.TooLong",
            $"Company name must be at most {CompanyName.MaxLength} characters.");

    public static readonly Error EmailEmpty =
        Error.Validation("Client.Email.Empty", "Contact email is required.");

    public static readonly Error EmailInvalid =
        Error.Validation("Client.Email.Invalid", "Contact email is not a valid email address.");

    public static readonly Error EmailTooLong =
        Error.Validation(
            "Client.Email.TooLong",
            $"Contact email must be at most {EmailAddress.MaxLength} characters.");

    public static readonly Error NotFound =
        Error.NotFound("Client.NotFound", "No client was found with the specified identifier.");

    public static readonly Error EmailAlreadyInUse =
        Error.Conflict("Client.Email.AlreadyInUse", "Another client already uses this contact email.");

    public static readonly Error AlreadyInactive =
        Error.Conflict("Client.AlreadyInactive", "The client is already inactive.");

    public static readonly Error Inactive =
        Error.Conflict("Client.Inactive", "The client is inactive and cannot be modified.");
}
