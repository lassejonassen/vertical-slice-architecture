using System.Net;
using Shouldly;
using VerticalSliceArchitecture.Api.Infrastructure.Security;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

/// <summary>
/// Every <c>/api/v1</c> route requires authorization by default (see <c>Program.cs</c>); these
/// tests pin that behaviour down using one representative endpoint rather than repeating the
/// same 401/403 assertions inside every feature's own test class.
/// </summary>
[Collection(ApiCollection.Name)]
public class AuthorizationTests(ApiFactory factory)
{
    [Fact]
    public async Task AnonymousRequest_ToAProtectedEndpoint_Returns401()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/clients/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithoutTheRequiredRole_Returns403()
    {
        HttpClient client = factory.CreateAuthenticatedClient(); // authenticated, no roles

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/clients/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithTheRequiredRole_IsNotRejectedByAuthorization()
    {
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.Reader);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/clients/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Authorization passes; the 404 below comes from the handler, not the auth pipeline.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
