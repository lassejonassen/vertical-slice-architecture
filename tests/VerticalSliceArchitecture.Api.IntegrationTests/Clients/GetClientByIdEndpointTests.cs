using System.Net;
using System.Net.Http.Json;
using Shouldly;
using VerticalSliceArchitecture.Api.Features.Clients.GetClientById;
using VerticalSliceArchitecture.Api.Features.Clients.RegisterClient;
using VerticalSliceArchitecture.Api.Infrastructure.Security;

namespace VerticalSliceArchitecture.Api.IntegrationTests.Clients;

[Collection(ApiCollection.Name)]
public class GetClientByIdEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task GetById_ForAnExistingClient_Returns200WithDetails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client =
            factory.CreateAuthenticatedClient(ApplicationRoles.ClientManager, ApplicationRoles.Reader);
        RegisterClientResponse registered = await RegisterAsync(client, "Acme Corp", cancellationToken);

        HttpResponseMessage response =
            await client.GetAsync($"/api/v1/clients/{registered.Id}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ClientDetailsResponse? details = await response.Content.ReadFromJsonAsync<ClientDetailsResponse>(
            TestJson.Options, cancellationToken);
        details.ShouldNotBeNull();
        details.Id.ShouldBe(registered.Id);
        details.CompanyName.ShouldBe("Acme Corp");
        details.Status.ShouldBe("Active");
        details.DeactivatedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_ForAnUnknownId_Returns404()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient client = factory.CreateAuthenticatedClient(ApplicationRoles.Reader);

        HttpResponseMessage response =
            await client.GetAsync($"/api/v1/clients/{Guid.NewGuid()}", cancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    internal static async Task<RegisterClientResponse> RegisterAsync(
        HttpClient client,
        string companyName,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/clients",
            new RegisterClientRequest(companyName, $"{Guid.NewGuid()}@acme.test"),
            TestJson.Options,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RegisterClientResponse>(
            TestJson.Options, cancellationToken))!;
    }
}
