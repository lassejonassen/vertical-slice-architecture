namespace VerticalSliceArchitecture.Domain.Users;

/// <summary>
/// The stable link between a local <see cref="User"/> and an account at the identity provider.
/// <para>
/// Both parts matter. <paramref name="Subject"/> alone is not unique across providers, and with
/// Entra ID it is not even stable across applications: the <c>sub</c> claim is pairwise, so the
/// same human presents a different <c>sub</c> to every app registration. The security layer
/// therefore builds this from <c>oid</c> (object ID) and <c>tid</c> (tenant ID) for Entra, and
/// from the issuer plus <c>sub</c> for Keycloak. See <c>ExternalIdentityFactory</c>.
/// </para>
/// <para>
/// A record <em>class</em> rather than a record struct, despite being a small value: EF Core owned
/// entity types must be reference types. Single-valued value objects such as <c>CompanyName</c>
/// map through a value converter instead and can stay structs.
/// </para>
/// </summary>
/// <param name="Issuer">The <c>iss</c> value, or the Entra tenant ID.</param>
/// <param name="Subject">The provider's immutable identifier for the account.</param>
public sealed record ExternalIdentity(string Issuer, string Subject)
{
    public override string ToString() => $"{Issuer}|{Subject}";
}

