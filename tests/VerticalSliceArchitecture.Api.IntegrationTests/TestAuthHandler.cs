using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

/// <summary>
/// Stands in for the real JWT bearer scheme in integration tests. Registered as the default
/// authentication/challenge scheme by <see cref="ApiFactory"/>, so tests control the caller's
/// identity through request headers instead of minting real access tokens.
/// <para>
/// No <see cref="TestAuthHandler.SubjectHeaderName"/> header means the request is anonymous —
/// <c>HandleAuthenticateAsync</c> returns <see cref="AuthenticateResult.NoResult"/>, which lets
/// <c>RequireAuthorization()</c> challenge (401) exactly as it would with a missing bearer token.
/// </para>
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Test";
    public const string SubjectHeaderName = "X-Test-Subject";
    public const string RolesHeaderName = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubjectHeaderName, out StringValues subjectHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims =
        [
            new Claim("oid", subjectHeader.ToString()),
            new Claim("tid", ApiFactory.TenantId),
            new Claim("name", "Test User"),
            new Claim("preferred_username", "test.user@example.test")
        ];

        if (Request.Headers.TryGetValue(RolesHeaderName, out StringValues rolesHeader))
        {
            claims.AddRange(rolesHeader.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
