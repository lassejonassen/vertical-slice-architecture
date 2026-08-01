using Microsoft.EntityFrameworkCore;

namespace VerticalSliceArchitecture.Api.Common.Database;

public static class DatabaseMigrationExtensions
{
	public static async Task MigrateDatabaseAsync(this IServiceProvider services)
	{
		using var scope = services.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		await dbContext.Database.MigrateAsync();
	}
}
