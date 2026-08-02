using VerticalSliceArchitecture.Api.Infrastructure.Endpoints;
using VerticalSliceArchitecture.Api.Infrastructure.Endpoints.Filters;
using VerticalSliceArchitecture.Api.Infrastructure.RateLimiting;
using VerticalSliceArchitecture.Api.Infrastructure.Security;
using VerticalSliceArchitecture.Domain.Users;

namespace VerticalSliceArchitecture.Api.Features.Users.GetCurrentUser;

public sealed record CurrentUserResponse(
    Guid Id,
    string DisplayName,
    string? Email,
    string Issuer,
    IReadOnlyCollection<string> Roles);

/// <summary>
/// The endpoint a SPA calls on startup to find out who it is talking as.
/// <para>
/// It has no handler at all — the work is done by <c>RequireProvisionedUserFilter</c>, and what
/// remains is a projection. Not every slice needs a command, a handler and a dispatcher; adding
/// them here would be ceremony around a two-line mapping.
/// </para>
/// </summary>
internal sealed class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("/users/me", (HttpContext httpContext, ICurrentUser currentUser) =>
        {
            User user = httpContext.GetProvisionedUser();

            return Results.Ok(new CurrentUserResponse(
                user.Id.Value,
                user.DisplayName,
                user.Email,
                user.Identity.Issuer,
                currentUser.Roles));
        })
            .WithName("GetCurrentUser")
            .WithSummary("Returns the authenticated user, provisioning them on first call.")
            .WithTags("Users")
            .RequireProvisionedUser()
            .RequireRateLimiting(RateLimitPolicies.PerUser)
            .Produces<CurrentUserResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
}
