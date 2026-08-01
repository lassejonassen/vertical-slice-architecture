using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using VerticalSliceArchitecture.Api.Common.Database;
using VerticalSliceArchitecture.Api.Common.Database.Interceptors;
using VerticalSliceArchitecture.Api.Common.Endpoints;
using VerticalSliceArchitecture.Api.Common.Messaging;
using VerticalSliceArchitecture.Api.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
	.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
	.AddEnvironmentVariables();

Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.CreateBootstrapLogger();

builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, services, configuration) =>
{
	configuration.ReadFrom.Configuration(context.Configuration)
		.ReadFrom.Services(services)
		.Enrich.FromLogContext()
		.Enrich.WithMachineName()
		.Enrich.WithThreadId();
}, writeToProviders: true);

var openTelemetry = builder.Services.AddOpenTelemetry()
	.ConfigureResource(cfg => cfg.AddService(builder.Environment.ApplicationName));
openTelemetry
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddConsoleExporter())
	.WithMetrics(metrics => metrics
		.AddAspNetCoreInstrumentation()
		.AddConsoleExporter());


builder.Services.AddScoped<ICorrelationContext, CorrelationContext>();

builder.Services.AddScoped<DispatchDomainEventsInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
	options
		.UseNpgsql(builder.Configuration.GetConnectionString("Database"))
		.AddInterceptors(sp.GetRequiredService<DispatchDomainEventsInterceptor>());
});

builder.Services.AddMediator();
builder.Services.AddMediatorHandlers(Assembly.GetExecutingAssembly());

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();

builder.Services.AddProblemDetails();

builder.Services.AddCors();

builder.Services.AddAuthorization();

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Migrate database
if (app.Environment.IsDevelopment())
{
	await app.Services.MigrateDatabaseAsync();
}

app.MapOpenApi();

app.MapScalarApiReference();

app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseCors(x =>
{
	if (app.Environment.IsDevelopment())
	{
		x.AllowAnyHeader();
		x.AllowAnyMethod();
		x.AllowAnyOrigin();
	}
	else
	{
		string[] allowedOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
		x.WithOrigins(allowedOrigins)
		 .AllowAnyHeader()
		 .AllowAnyMethod()
		 .AllowCredentials();
	}
});

app.UseAuthorization();

app.MapGet("/", () => "Always on");
app.MapEndpoints();

app.Run();

// Exposes the generated entry point so WebApplicationFactory<Program> can host it in tests.
public partial class Program;
