using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Domain.Clients;

/// <summary>
/// A validated company name. Construction goes through <see cref="Create"/>, so an instance of
/// this type is proof that the value is legal — no defensive re-checking downstream.
/// </summary>
public readonly record struct CompanyName
{
    public const int MaxLength = 200;

    private CompanyName(string value) => Value = value;

    public string Value { get; }

    public static Result<CompanyName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ClientErrors.CompanyNameEmpty;
        }

        string trimmed = value.Trim();

        return trimmed.Length > MaxLength
            ? ClientErrors.CompanyNameTooLong
            : new CompanyName(trimmed);
    }

    public override string ToString() => Value;
}
