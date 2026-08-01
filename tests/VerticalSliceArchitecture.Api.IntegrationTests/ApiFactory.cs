using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

// Runs the real API against a real, disposable Postgres so the whole pipeline
// (EF mappings, migrations, mediator, validation, endpoints) is exercised end-to-end.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
	private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
		.WithDatabase("vertical_slice_architecture")
		.WithUsername("postgres")
		.WithPassword("postgres")
		.Build();

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Development");

		builder.ConfigureAppConfiguration((_, configBuilder) =>
		{
			configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:Database"] = _postgres.GetConnectionString(),
			});
		});
	}

	public Task InitializeAsync() => _postgres.StartAsync();

	public new async Task DisposeAsync()
	{
		await _postgres.DisposeAsync();
		await base.DisposeAsync();
	}
}
