using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using VerticalSliceArchitecture.Api.Infrastructure.Configuration;
using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Messaging;
using VerticalSliceArchitecture.Api.Infrastructure.Middleware;
using VerticalSliceArchitecture.Api.Infrastructure.Observability;
using VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;
using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.Persistence;
using VerticalSliceArchitecture.SharedKernel.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.AddAzureAppConfiguration();

builder.Host.UseApplicationSerilog();

var assembly = typeof(Program).Assembly;

builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

builder.Services.AddApplicationObservability(builder.Configuration, builder.Environment);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddApplicationSecurity(builder.Configuration);
builder.Services.AddApplicationRateLimiting(builder.Configuration);

builder.Services.AddMessaging(assembly);
builder.Services.AddDomainEventHandlers(assembly);
builder.Services.AddEndpoints(assembly);
builder.Services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

builder.Services.AddProblemDetails(options =>
     options.CustomizeProblemDetails = context =>
     {
         context.ProblemDetails.Instance =
             $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
         context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
     });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Populate these from configuration in production. Left empty, the middleware trusts any
    // proxy, which lets a caller spoof their IP and defeat IP-partitioned rate limiting.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

await app.Services.ApplyMigrationsAsync();

app.UseForwardedHeaders();
app.UseCorrelationId();
app.UseSecurityHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseApplicationRequestLogging();
app.UseAzureAppConfigurationRefresh(builder.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

RouteGroupBuilder api = app
        .MapGroup("/api/v1")
        .RequireAuthorization();

app.MapEndpoints(api);
// Liveness answers "is the process up"; readiness answers "can it serve traffic". A single
// combined probe makes Kubernetes restart a pod whose database is briefly unreachable, which
// turns a short outage into a crash loop.
app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();

app.MapHealthChecks("/health").AllowAnonymous();

await app.RunAsync();

// Exposes the generated entry point so WebApplicationFactory<Program> can host it in tests.
public partial class Program;
