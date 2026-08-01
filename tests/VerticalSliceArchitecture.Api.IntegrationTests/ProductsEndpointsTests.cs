using System.Net;
using System.Net.Http.Json;
using VerticalSliceArchitecture.Api.Features.Products.CreateProduct;
using VerticalSliceArchitecture.Api.Features.Products.GetProductById;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ProductsEndpointsTests(ApiFactory factory)
{
	private readonly HttpClient _client = factory.CreateClient();

	[Fact]
	public async Task CreateProduct_WithValidRequest_Returns201WithProduct()
	{
		var request = new CreateProductRequest("Widget", 9.99m);

		var response = await _client.PostAsJsonAsync("/api/products", request);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<CreateProductResponse>(TestJson.Options);
		Assert.NotNull(body);
		Assert.Equal("Widget", body.Name);
		Assert.Equal(9.99m, body.Price);
		Assert.NotEqual(Guid.Empty, body.Id);
		Assert.Equal($"/api/products/{body.Id}", response.Headers.Location?.ToString());
	}

	[Theory]
	[InlineData("", 1)]
	[InlineData("Widget", 0)]
	public async Task CreateProduct_WithInvalidRequest_Returns400ProblemDetails(string name, decimal price)
	{
		var request = new CreateProductRequest(name, price);

		var response = await _client.PostAsJsonAsync("/api/products", request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(TestJson.Options);
		Assert.NotNull(problem);
		Assert.Equal(400, problem.Status);
	}

	[Fact]
	public async Task GetProductById_WhenProductExists_Returns200WithProduct()
	{
		var created = await CreateProductAsync("Gadget", 19.99m);

		var response = await _client.GetAsync($"/api/products/{created.Id}");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<ProductDetailsDto>(TestJson.Options);
		Assert.NotNull(body);
		Assert.Equal(created.Id, body.Id);
		Assert.Equal("Gadget", body.Name);
		Assert.Equal(19.99m, body.Price);
	}

	[Fact]
	public async Task GetProductById_WhenProductDoesNotExist_Returns404ProblemDetails()
	{
		var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
		var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(TestJson.Options);
		Assert.NotNull(problem);
		Assert.Equal(404, problem.Status);
	}

	private async Task<CreateProductResponse> CreateProductAsync(string name, decimal price)
	{
		var response = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(name, price));
		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<CreateProductResponse>(TestJson.Options))!;
	}
}
