using System.Security.Claims;
using Microsoft.Extensions.Options;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

/// <summary>
/// Extracts a stable <see cref="ExternalIdentity"/> from the current principal.
/// <para>
/// This is the one piece of the security layer that genuinely differs between the two providers,
/// and getting it wrong is subtle rather than loud.
/// </para>
/// <para>
/// <b>Entra ID.</b> Do not use <c>sub</c>. It is pairwise: the same person presents a different
/// <c>sub</c> to each app registration, so keying local users on it means a second application —
/// or a re-registered one — no longer recognises anybody. Use <c>oid</c> (immutable object ID)
/// scoped by <c>tid</c> (tenant ID). Note also that <c>oid</c> differs for a guest user between
/// their home tenant and the resource tenant, which is exactly why the tenant belongs in the key.
/// </para>
/// <para>
/// <b>Keycloak.</b> <c>sub</c> is the realm-scoped user ID and is stable, so issuer plus subject
/// is correct there.
/// </para>
/// </summary>
public interface IExternalIdentityFactory
{
    public Result<ExternalIdentity> Create(ClaimsPrincipal principal);
}

internal sealed class ExternalIdentityFactory(IOptions<SecurityOptions> options) : IExternalIdentityFactory
{
    public const string ObjectIdClaim = "oid";
    public const string TenantIdClaim = "tid";

    private const string EntraObjectIdUri =
        "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private const string EntraTenantIdUri =
        "http://schemas.microsoft.com/identity/claims/tenantid";

    private readonly SecurityOptions _options = options.Value;

    public Result<ExternalIdentity> Create(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return UserErrors.NotAuthenticated;
        }

        return _options.Provider switch
        {
            IdentityProviderKind.EntraId => CreateForEntra(principal),
            IdentityProviderKind.Keycloak => CreateForKeycloak(principal),
            _ => UserErrors.ExternalIdentityMissing
        };
    }

    private Result<ExternalIdentity> CreateForEntra(ClaimsPrincipal principal)
    {
        string? objectId = principal.FindFirstValue(ObjectIdClaim)
                           ?? principal.FindFirstValue(EntraObjectIdUri);

        string? tenantId = principal.FindFirstValue(TenantIdClaim)
                           ?? principal.FindFirstValue(EntraTenantIdUri)
                           ?? _options.TenantId;

        if (string.IsNullOrWhiteSpace(objectId) || string.IsNullOrWhiteSpace(tenantId))
        {
            return UserErrors.ExternalIdentityMissing;
        }

        return new ExternalIdentity($"entra:{tenantId}", objectId);
    }

    private static Result<ExternalIdentity> CreateForKeycloak(ClaimsPrincipal principal)
    {
        string? subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? principal.FindFirstValue("sub");

        string? issuer = principal.FindFirst("iss")?.Value
                         ?? principal.Claims.FirstOrDefault()?.Issuer;

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(issuer))
        {
            return UserErrors.ExternalIdentityMissing;
        }

        return new ExternalIdentity(issuer, subject);
    }
}
