using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

/// <summary>
/// Hosts the real API in-process against a disposable Postgres container (via Testcontainers —
/// Docker must be running) and swaps the JWT bearer scheme for <see cref="TestAuthHandler"/>, so
/// tests authenticate by header instead of minting real tokens.
/// <para>
/// One instance is shared across a test class via <see cref="ApiCollection"/> so the container
/// only starts once per class; individual tests stay isolated from each other by using a fresh
/// subject (and, where it matters, a fresh email) per test rather than by resetting the database.
/// </para>
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TenantId = "9f16c6e4-6b8b-4a63-9f3a-9c8f7f1e2b60";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .Build();

    /// <summary>
    /// An <see cref="HttpClient"/> authenticated as a fresh test principal. Pass <paramref name="roles"/>
    /// to grant application roles (see <c>ApplicationRoles</c>); omit them to get an authenticated
    /// caller with no roles, for asserting 403 on role-gated endpoints.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        HttpClient client = CreateClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeaderName, Guid.NewGuid().ToString());

        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeaderName, string.Join(',', roles));
        }

        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "PostgreSql",
                ["Persistence:ConnectionString"] = _postgres.GetConnectionString(),
                ["Persistence:MigrateOnStartup"] = "true",
                ["Security:Provider"] = "EntraId",
                ["Security:Authority"] = $"https://login.microsoftonline.com/{TenantId}/v2.0",
                ["Security:Audience"] = "test-audience",
                ["Security:TenantId"] = TenantId,
                ["Observability:OtlpEndpoint"] = string.Empty
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    public ValueTask InitializeAsync() => new(_postgres.StartAsync());

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
