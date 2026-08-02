namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

/// <summary>
/// Named policies, referenced by endpoints via <c>RequireAuthorization(AuthorizationPolicies.X)</c>.
/// <para>
/// Endpoints name a policy, never a role. Roles are an implementation detail of the identity
/// provider — they get renamed, they differ between Entra app roles and Keycloak realm roles, and
/// they change when a customer restructures their directory. A policy name is a statement about
/// what the endpoint needs, which is stable.
/// </para>
/// </summary>
public static class AuthorizationPolicies
{
    public const string RequireApiAccess = nameof(RequireApiAccess);
    public const string ManageClients = nameof(ManageClients);
    public const string ReadClients = nameof(ReadClients);
}

public static class ApplicationRoles
{
    public const string Administrator = "acme.administrator";
    public const string ClientManager = "acme.client-manager";
    public const string Reader = "acme.reader";
}
