using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.Domain.Users;
using VerticalSliceArchitecture.SharedKernel.Results;

namespace VerticalSliceArchitecture.Api.Infrastructure.Endpoints.Filters;

/// <summary>
/// Ensures the caller has a provisioned local <see cref="User"/> and puts it in
/// <c>HttpContext.Items</c> for the handler.
/// <para>
/// Example of the second thing filters are good for: turning an authenticated principal into
/// domain state once, rather than repeating the lookup in every handler that needs it.
/// </para>
/// </summary>
public sealed class RequireProvisionedUserFilter(IUserProvisioningService provisioning) : IEndpointFilter
{
    public const string HttpContextItemKey = "Acme.CurrentUser";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        Result<User> user = await provisioning.GetOrProvisionCurrentUserAsync(
            context.HttpContext.RequestAborted);

        if (user.IsFailure)
        {
            return user.ToProblemDetails();
        }

        context.HttpContext.Items[HttpContextItemKey] = user.Value;

        return await next(context);
    }
}

public static class ProvisionedUserExtensions
{
    public static RouteHandlerBuilder RequireProvisionedUser(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<RequireProvisionedUserFilter>()
            .RequireAuthorization();

    public static User GetProvisionedUser(this HttpContext context) =>
        context.Items[RequireProvisionedUserFilter.HttpContextItemKey] as User
        ?? throw new InvalidOperationException(
            "No provisioned user on the request. Did you forget RequireProvisionedUser()?");
}
