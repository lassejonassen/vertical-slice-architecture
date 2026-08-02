using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace VerticalSliceArchitecture.Api.Infrastructure.Configuration;

public static class OptionsValidationExtensions
{
    /// <summary>
    /// Binds and validates an options type, failing at startup rather than at first use.
    /// <para>
    /// <c>ValidateOnStart</c> is the important half. Without it a missing connection string
    /// surfaces as an exception on whichever request first needs the database — typically in
    /// production, typically at 3am, and typically nowhere near the deployment that caused it.
    /// </para>
    /// </summary>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class =>
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => Validator.TryValidateObject(options, new ValidationContext(options), null, true),
                $"Configuration section '{sectionName}' is invalid.")
            .ValidateOnStart();
}
