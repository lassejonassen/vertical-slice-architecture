using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using VerticalSliceArchitecture.Persistence.Interceptors;

namespace VerticalSliceArchitecture.Api.Infrastructure.Security;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures bearer token validation for whichever provider is selected.
    /// <para>
    /// This is an API, so it validates tokens rather than running the OIDC code flow. There is no
    /// cookie, no redirect and no <c>/signin-oidc</c> callback: the browser app owns the login
    /// dance and arrives here with an access token. Adding the interactive handler to an API is a
    /// common and expensive detour.
    /// </para>
    /// <para>
    /// The userinfo endpoint is likewise not called. Claims come from the validated access token;
    /// calling userinfo with an access token audienced for this API returns 401 from the provider,
    /// because that endpoint expects a token audienced for the provider itself.
    /// </para>
    /// </summary>
    public static IServiceCollection AddApplicationSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        SecurityOptions options = configuration
            .GetSection(SecurityOptions.SectionName)
            .Get<SecurityOptions>() ?? new SecurityOptions();

        services.AddHttpContextAccessor();
        services.AddScoped<IExternalIdentityFactory, ExternalIdentityFactory>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        if (options.Provider == IdentityProviderKind.Keycloak)
        {
            services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt => ConfigureJwtBearer(jwt, options));

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(BuildApiAccessPolicy(options))
            .AddPolicy(AuthorizationPolicies.RequireApiAccess, BuildApiAccessPolicy(options))
            .AddPolicy(AuthorizationPolicies.ReadClients, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(ApplicationRoles.Reader, ApplicationRoles.ClientManager, ApplicationRoles.Administrator))
            .AddPolicy(AuthorizationPolicies.ManageClients, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(ApplicationRoles.ClientManager, ApplicationRoles.Administrator));

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions jwt, SecurityOptions options)
    {
        jwt.Authority = options.Authority;
        jwt.Audience = options.Audience;
        jwt.RequireHttpsMetadata = options.RequireHttpsMetadata;
        jwt.MapInboundClaims = false;

        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(options.ClockSkewSeconds),
            NameClaimType = options.Provider == IdentityProviderKind.EntraId
                ? "name"
                : "preferred_username",
            RoleClaimType = ClaimTypes.Role,
            ValidAudiences = options.ValidAudiences.Length > 0
                ? [options.Audience, .. options.ValidAudiences]
                : [options.Audience]
        };

        if (options.Provider == IdentityProviderKind.EntraId)
        {
            // Entra v2 tokens carry an issuer containing the tenant GUID even when the authority
            // was configured with a domain name, so accept both forms.
            jwt.TokenValidationParameters.ValidIssuers =
            [
                $"https://login.microsoftonline.com/{options.TenantId}/v2.0",
                $"https://sts.windows.net/{options.TenantId}/"
            ];
        }

        jwt.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Surfaced as a response header only outside production; the detail is useful during
                // integration but tells an attacker why their forged token was rejected.
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("token-expired", "true");
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                return context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                    title = "Unauthorized",
                    status = StatusCodes.Status401Unauthorized,
                    detail = "A valid access token is required.",
                    errorCode = "User.NotAuthenticated",
                    traceId = context.HttpContext.TraceIdentifier
                });
            }
        };
    }

    private static AuthorizationPolicy BuildApiAccessPolicy(SecurityOptions options)
    {
        AuthorizationPolicyBuilder builder = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser();

        if (!string.IsNullOrWhiteSpace(options.RequiredScope))
        {
            builder.RequireAssertion(context => HasScope(context.User, options.RequiredScope));
        }

        return builder.Build();
    }

    private static bool HasScope(ClaimsPrincipal principal, string requiredScope) =>
        principal
            .FindAll(claim => claim.Type is "scp" or "scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(requiredScope, StringComparer.Ordinal);
}
