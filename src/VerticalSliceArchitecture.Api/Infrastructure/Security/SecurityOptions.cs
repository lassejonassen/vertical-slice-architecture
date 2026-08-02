using System.ComponentModel.DataAnnotations;

namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

public enum IdentityProviderKind
{
    EntraId = 0,
    Keycloak = 1
}

/// <summary>
/// Bound from the <c>Security</c> section and validated at startup. A misconfigured authority is
/// the kind of mistake that otherwise surfaces as a puzzling 401 in production.
/// </summary>
public sealed class SecurityOptions : IValidatableObject
{
    public const string SectionName = "Security";

    public IdentityProviderKind Provider { get; init; } = IdentityProviderKind.EntraId;

    /// <summary>
    /// OIDC authority. Entra: <c>https://login.microsoftonline.com/{tenantId}/v2.0</c>.
    /// Keycloak: <c>https://{host}/realms/{realm}</c>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// Expected <c>aud</c>. For Entra this is the API's Application ID URI (or client ID) — and it
    /// must match the resource the client requested a token for. A token issued for Microsoft Graph
    /// will not validate here, which is the usual cause of a 401 that "should" work.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Entra tenant ID. Used to form the identity issuer for pairwise subjects.</summary>
    public string? TenantId { get; init; }

    /// <summary>Keycloak realm. Used to read realm roles out of the token.</summary>
    public string? Realm { get; init; }

    /// <summary>Additional accepted audiences, e.g. during a client ID migration.</summary>
    public string[] ValidAudiences { get; init; } = [];

    /// <summary>Leave enabled everywhere except against a local Keycloak over plain HTTP.</summary>
    public bool RequireHttpsMetadata { get; init; } = true;

    /// <summary>Clock skew allowance in seconds. Keep small; the default of five minutes is generous.</summary>
    [Range(0, 300)]
    public int ClockSkewSeconds { get; init; } = 30;

    /// <summary>Scope required by every protected endpoint, e.g. <c>api.access</c>.</summary>
    public string? RequiredScope { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Provider == IdentityProviderKind.EntraId && string.IsNullOrWhiteSpace(TenantId))
        {
            yield return new ValidationResult(
                "Security:TenantId is required when the provider is EntraId, because the tenant "
                + "scopes the pairwise subject identifier.",
                [nameof(TenantId)]);
        }

        if (Provider == IdentityProviderKind.Keycloak && string.IsNullOrWhiteSpace(Realm))
        {
            yield return new ValidationResult(
                "Security:Realm is required when the provider is Keycloak.",
                [nameof(Realm)]);
        }
    }
}
