using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VerticalSliceArchitecture.Domain.Clients;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.Persistence.Interceptors;
using VerticalSliceArchitecture.Persistence.Repositories;

namespace VerticalSliceArchitecture.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

        // Interceptors are scoped because they depend on the current user.
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, builder) =>
        {
            PersistenceOptions options = serviceProvider
                .GetRequiredService<IOptions<PersistenceOptions>>()
                .Value;

            ConfigureProvider(builder, options);

            builder.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>());

            if (options.EnableSensitiveDataLogging)
            {
                builder.EnableSensitiveDataLogging().EnableDetailedErrors();
            }
        });

        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }

    private static void ConfigureProvider(DbContextOptionsBuilder builder, PersistenceOptions options)
    {
        switch (options.Provider)
        {
            case DatabaseProvider.PostgreSql:
                builder.UseNpgsql(options.ConnectionString, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__migrations", ApplicationDbContext.DefaultSchema);
                    npgsql.EnableRetryOnFailure(options.MaxRetryCount);
                    npgsql.CommandTimeout(options.CommandTimeoutSeconds);
                });

                // snake_case everywhere is the PostgreSQL convention and avoids quoting every
                // identifier. Explicit column names in the configurations already follow it.
                builder.UseSnakeCaseNamingConvention();
                break;

            case DatabaseProvider.SqlServer:
                builder.UseSqlServer(options.ConnectionString, sqlServer =>
                {
                    sqlServer.MigrationsHistoryTable("__migrations", ApplicationDbContext.DefaultSchema);
                    sqlServer.EnableRetryOnFailure(options.MaxRetryCount);
                    sqlServer.CommandTimeout(options.CommandTimeoutSeconds);
                });
                break;

            default:
                throw new InvalidOperationException($"Unsupported database provider '{options.Provider}'.");
        }
    }

    /// <summary>
    /// Applies migrations when configured to. Note that this serialises badly across replicas —
    /// for anything beyond a single instance, run migrations as a separate deployment step instead.
    /// </summary>
    public static async Task ApplyMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();

        PersistenceOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<PersistenceOptions>>()
            .Value;

        if (!options.MigrateOnStartup)
        {
            return;
        }

        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }
}
