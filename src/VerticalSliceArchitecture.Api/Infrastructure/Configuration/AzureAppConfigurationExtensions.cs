using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

namespace VerticalSliceArchitecture.Api.Infrastructure.Configuration;

public static class AzureAppConfigurationExtensions
{
    /// <summary>Sentinel key. Bump this one value to trigger a refresh of everything else.</summary>
    public const string SentinelKey = "Acme:Settings:Sentinel";

    /// <summary>
    /// Layers Azure App Configuration over the local providers.
    /// <para>
    /// Two details worth knowing. First, refresh is driven by a <em>sentinel</em>: only that key is
    /// polled, and a change to it invalidates the whole snapshot. Polling every key individually
    /// costs a request per key per interval and gives you a torn read halfway through a multi-key
    /// change, which is worse than not refreshing at all.
    /// </para>
    /// <para>
    /// Second, refreshed values only reach code that reads through <c>IOptionsMonitor&lt;T&gt;</c>.
    /// <c>IOptions&lt;T&gt;</c> is resolved once and never updated, so a hot-reloadable setting
    /// injected as <c>IOptions</c> will silently keep its startup value forever.
    /// </para>
    /// <para>
    /// Absent configuration is not an error: the API runs from appsettings and user secrets alone,
    /// which keeps local development and CI free of an Azure dependency.
    /// </para>
    /// </summary>
    public static WebApplicationBuilder AddAzureAppConfiguration(this WebApplicationBuilder builder)
    {
        string? endpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
        string? connectionString = builder.Configuration["AzureAppConfiguration:ConnectionString"];

        if (string.IsNullOrWhiteSpace(endpoint) && string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        string label = builder.Configuration["AzureAppConfiguration:Label"]
                       ?? builder.Environment.EnvironmentName;

        int refreshSeconds = builder.Configuration.GetValue("AzureAppConfiguration:RefreshIntervalSeconds", 30);

        builder.Configuration.AddAzureAppConfiguration(config =>
        {
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                // Managed identity in Azure, developer credentials locally. No secrets in config.
                var credential = new DefaultAzureCredential();

                config.Connect(new Uri(endpoint), credential);

                // Key Vault references are resolved by this provider, so a secret looks like any
                // other setting to the rest of the application.
                config.ConfigureKeyVault(keyVault => keyVault.SetCredential(credential));
            }
            else
            {
                config.Connect(connectionString);
            }

            // Unlabelled keys first, then environment-specific ones, which therefore win.
            config
                .Select(KeyFilter.Any, LabelFilter.Null)
                .Select(KeyFilter.Any, label);

            config.ConfigureRefresh(refresh => refresh
                .Register(SentinelKey, label, refreshAll: true)
                .SetRefreshInterval(TimeSpan.FromSeconds(refreshSeconds)));

            config.UseFeatureFlags(flags =>
            {
                flags.Select(KeyFilter.Any, label);
                flags.SetRefreshInterval(TimeSpan.FromSeconds(refreshSeconds));
            });
        });

        builder.Services.AddAzureAppConfiguration();
        builder.Services.AddFeatureManagement();

        return builder;
    }

    /// <summary>
    /// Adds the middleware that actually performs the refresh check. Without it the configured
    /// refresh interval does nothing, because the provider only polls when a request drives it.
    /// </summary>
    public static IApplicationBuilder UseAzureAppConfigurationRefresh(
        this IApplicationBuilder app,
        IConfiguration configuration)
    {
        bool configured = !string.IsNullOrWhiteSpace(configuration["AzureAppConfiguration:Endpoint"])
                          || !string.IsNullOrWhiteSpace(configuration["AzureAppConfiguration:ConnectionString"]);

        return configured ? app.UseAzureAppConfigurationRefresh(configuration) : app;
    }
}
