using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VerticalSliceArchitecture.Api.Features.Clients.GetClientById;
using VerticalSliceArchitecture.Api.Features.Clients.RegisterClient;
using VerticalSliceArchitecture.Api.Infrastructure.Security;

namespace VerticalSliceArchitecture.Api.IntegrationTests.Clients;

[Collection(ApiCollection.Name)]
public class DeactivateClientEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task Deactivate_AnActiveClient_Returns204AndTheClientShowsAsInactive()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);
        RegisterClientResponse registered =
            await GetClientByIdEndpointTests.RegisterAsync(client, "Acme Corp", cancellationToken);

        HttpResponseMessage deactivate = await client.PostAsync(
            $"/api/v1/clients/{registered.Id}/deactivate", content: null, cancellationToken);
        deactivate.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse =
            await client.GetAsync($"/api/v1/clients/{registered.Id}", cancellationToken);
        ClientDetailsResponse? details = await getResponse.Content.ReadFromJsonAsync<ClientDetailsResponse>(
            TestJson.Options, cancellationToken);
        details.ShouldNotBeNull();
        details.Status.ShouldBe("Inactive");
        details.DeactivatedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task Deactivate_AnAlreadyInactiveClient_Returns409Conflict()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);
        RegisterClientResponse registered =
            await GetClientByIdEndpointTests.RegisterAsync(client, "Acme Corp", cancellationToken);
        await client.PostAsync($"/api/v1/clients/{registered.Id}/deactivate", content: null, cancellationToken);

        HttpResponseMessage second = await client.PostAsync(
            $"/api/v1/clients/{registered.Id}/deactivate", content: null, cancellationToken);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deactivate_AnUnknownId_Returns404()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);

        HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/clients/{Guid.NewGuid()}/deactivate", content: null, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_WithoutTheManageClientsRole_Returns403()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient owner = factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager);
        RegisterClientResponse registered =
            await GetClientByIdEndpointTests.RegisterAsync(owner, "Acme Corp", cancellationToken);
        HttpClient reader = factory.CreateAuthenticatedClient(ApplicationRoles.Reader);

        HttpResponseMessage response = await reader.PostAsync(
            $"/api/v1/clients/{registered.Id}/deactivate", content: null, cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
