using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

/// <summary>
/// Flattens Keycloak's nested role claims into ordinary <see cref="ClaimTypes.Role"/> claims.
/// <para>
/// Keycloak emits roles as JSON objects — <c>realm_access.roles</c> and
/// <c>resource_access.{client}.roles</c> — which ASP.NET Core's JWT handler stores verbatim as a
/// single string claim. Without this, <c>User.IsInRole</c> and every role-based policy silently
/// return false, which looks like a permissions bug rather than a parsing one.
/// </para>
/// <para>
/// Entra needs no equivalent: it emits a flat <c>roles</c> array that maps directly.
/// </para>
/// </summary>
internal sealed class KeycloakRolesClaimsTransformation(IOptions<SecurityOptions> options)
    : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string ResourceAccessClaim = "resource_access";

    private readonly SecurityOptions _options = options.Value;

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (_options.Provider != IdentityProviderKind.Keycloak
            || principal.Identity is not ClaimsIdentity identity
            || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        // TransformAsync can run more than once per request in some pipelines, so this must be idempotent.
        if (identity.HasClaim(claim => claim.Type == TransformedMarker))
        {
            return Task.FromResult(principal);
        }

        foreach (string role in ReadRealmRoles(principal).Concat(ReadClientRoles(principal)))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        identity.AddClaim(new Claim(TransformedMarker, "true"));

        return Task.FromResult(principal);
    }

    private const string TransformedMarker = "acme:roles_transformed";

    private static IEnumerable<string> ReadRealmRoles(ClaimsPrincipal principal)
    {
        string? realmAccess = principal.FindFirstValue(RealmAccessClaim);

        return realmAccess is null ? [] : ReadRolesArray(realmAccess, "roles");
    }

    private IEnumerable<string> ReadClientRoles(ClaimsPrincipal principal)
    {
        string? resourceAccess = principal.FindFirstValue(ResourceAccessClaim);

        if (resourceAccess is null || string.IsNullOrWhiteSpace(_options.Audience))
        {
            return [];
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(resourceAccess);

            return document.RootElement.TryGetProperty(_options.Audience, out JsonElement client)
                   && client.TryGetProperty("roles", out JsonElement roles)
                ? [.. roles.EnumerateArray().Select(role => role.GetString()).OfType<string>()]
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IEnumerable<string> ReadRolesArray(string json, string propertyName)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);

            return document.RootElement.TryGetProperty(propertyName, out JsonElement roles)
                ? [.. roles.EnumerateArray().Select(role => role.GetString()).OfType<string>()]
                : [];
        }
        catch (JsonException)
        {
            // A malformed claim should degrade to "no roles", not blow up the request.
            return [];
        }
    }
}
