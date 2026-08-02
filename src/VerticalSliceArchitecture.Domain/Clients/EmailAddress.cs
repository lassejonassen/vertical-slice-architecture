using System.Net.Mail;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Clients;

/// <summary>
/// A syntactically valid email address, normalised to lower case so that lookups and uniqueness
/// checks behave consistently regardless of how the caller typed it.
/// </summary>
public readonly record struct EmailAddress
{
    public const int MaxLength = 320;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static Result<EmailAddress> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ClientErrors.EmailEmpty;
        }

        string normalised = value.Trim().ToLowerInvariant();

        if (normalised.Length > MaxLength)
        {
            return ClientErrors.EmailTooLong;
        }

        // MailAddress is stricter and better maintained than any regex we would write here.
        return MailAddress.TryCreate(normalised, out _)
            ? new EmailAddress(normalised)
            : ClientErrors.EmailInvalid;
    }

    public override string ToString() => Value;
}
