using System.Net;
using Shouldly;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class HealthEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Alive_IsAnonymousAndAlwaysHealthy()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/alive", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_IsAnonymousAndReportsTheDatabaseAsHealthy()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
