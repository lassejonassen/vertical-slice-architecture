using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VerticalSliceArchitecture.Api.Features.Clients.RegisterClient;
using VerticalSliceArchitecture.Api.Infrastructure.Security;

namespace VerticalSliceArchitecture.Api.IntegrationTests.Clients;

[Collection(ApiCollection.Name)]
public class RegisterClientEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Register_WithAValidRequest_Returns201WithLocationAndBody()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);
        var request = new RegisterClientRequest("Acme Corp", $"{Guid.NewGuid()}@acme.test");

        HttpResponseMessage response =
            await client.PostAsJsonAsync("/api/v1/clients", request, TestJson.Options, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        RegisterClientResponse? body = await response.Content.ReadFromJsonAsync<RegisterClientResponse>(
            TestJson.Options, cancellationToken);
        body.ShouldNotBeNull();
        body.CompanyName.ShouldBe("Acme Corp");
        body.ContactEmail.ShouldBe(request.ContactEmail);
        response.Headers.Location!.ToString().ShouldContain(body.Id.ToString());
    }

    [Fact]
    public async Task Register_WithAnInvalidEmail_Returns400ValidationProblem()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);
        var request = new RegisterClientRequest("Acme Corp", "not-an-email");

        HttpResponseMessage response =
            await client.PostAsJsonAsync("/api/v1/clients", request, TestJson.Options, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithAnEmailAlreadyInUse_Returns409Conflict()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);
        string email = $"{Guid.NewGuid()}@acme.test";

        HttpResponseMessage first = await client.PostAsJsonAsync(
            "/api/v1/clients", new RegisterClientRequest("Acme Corp", email), TestJson.Options, cancellationToken);
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        HttpResponseMessage second = await client.PostAsJsonAsync(
            "/api/v1/clients", new RegisterClientRequest("Other Corp", email), TestJson.Options, cancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithoutTheManageClientsRole_Returns403()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.Reader);
        var request = new RegisterClientRequest("Acme Corp", $"{Guid.NewGuid()}@acme.test");

        HttpResponseMessage response =
            await client.PostAsJsonAsync("/api/v1/clients", request, TestJson.Options, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
