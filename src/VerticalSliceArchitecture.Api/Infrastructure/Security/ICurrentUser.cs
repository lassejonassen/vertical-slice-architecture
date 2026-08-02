using System.Security.Claims;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.Persistence.Interceptors;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

/// <summary>
/// The authenticated caller, as far as the application is concerned. Handlers depend on this rather
/// than on <c>IHttpContextAccessor</c>, which keeps them testable and keeps claim-name trivia in
/// one file.
/// </summary>
public interface ICurrentUser
{
    public bool IsAuthenticated { get; }

    public Result<ExternalIdentity> Identity { get; }

    public string? DisplayName { get; }

    public string? Email { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public IReadOnlyCollection<string> Scopes { get; }

    public bool IsInRole(string role);

    public bool HasScope(string scope);
}

internal sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    IExternalIdentityFactory identityFactory) : ICurrentUser, ICurrentUserAccessor
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Result<ExternalIdentity> Identity => Principal is null
        ? UserErrors.NotAuthenticated
        : identityFactory.Create(Principal);

    public string? DisplayName =>
        Principal?.FindFirstValue("name")
        ?? Principal?.FindFirstValue(ClaimTypes.Name)
        ?? Principal?.FindFirstValue("preferred_username");

    public string? Email =>
        Principal?.FindFirstValue("email")
        ?? Principal?.FindFirstValue(ClaimTypes.Email)
        // Entra frequently omits `email` and puts the address in `preferred_username` instead.
        ?? Principal?.FindFirstValue("preferred_username");

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray() ?? [];

    public IReadOnlyCollection<string> Scopes =>
        Principal?
            .FindAll(claim => claim.Type is "scp" or "scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToArray() ?? [];

    /// <summary>Used by the audit interceptor, which only needs a name to stamp.</summary>
    public string? UserName => DisplayName ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.Ordinal);
}
