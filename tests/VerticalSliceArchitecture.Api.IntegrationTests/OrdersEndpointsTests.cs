using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VerticalSliceArchitecture.Api.Common.Database;
using VerticalSliceArchitecture.Api.Domain.Common.Enums;
using VerticalSliceArchitecture.Api.Domain.Common.StronglyTypedIds;
using VerticalSliceArchitecture.Api.Domain.Entities;
using VerticalSliceArchitecture.Api.Features.Orders.CreateOrder;
using VerticalSliceArchitecture.Api.Features.Products.CreateProduct;

namespace VerticalSliceArchitecture.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OrdersEndpointsTests(ApiFactory factory)
{
	private readonly HttpClient _client = factory.CreateClient();

	[Fact]
	public async Task CreateOrder_WithValidRequest_Returns201WithCorrectTotal()
	{
		var product1 = await CreateProductAsync("Widget", 10m);
		var product2 = await CreateProductAsync("Gadget", 5m);
		var request = new CreateOrderRequest(Guid.NewGuid(),
		[
			new CreateOrderRequestItem(product1.Id, 2),
			new CreateOrderRequestItem(product2.Id, 1),
		]);

		var response = await _client.PostAsJsonAsync("/api/orders", request);

		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		var body = await response.Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options);
		Assert.NotNull(body);
		Assert.Equal(25m, body.TotalAmount);
		Assert.Equal("Pending", body.Status);
		Assert.Equal(2, body.Items.Count);
	}

	[Fact]
	public async Task CreateOrder_WithUnknownProduct_Returns404ProblemDetails()
	{
		var request = new CreateOrderRequest(Guid.NewGuid(), [new CreateOrderRequestItem(Guid.NewGuid(), 1)]);

		var response = await _client.PostAsJsonAsync("/api/orders", request);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task CreateOrder_WithNoItems_Returns400ProblemDetails()
	{
		var request = new CreateOrderRequest(Guid.NewGuid(), []);

		var response = await _client.PostAsJsonAsync("/api/orders", request);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task CancelOrder_WhenPending_Returns204()
	{
		var order = await CreateOrderAsync();

		var response = await _client.PostAsync($"/api/orders/{order.Id}/cancel", content: null);

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact]
	public async Task CancelOrder_WhenAlreadyCancelled_IsIdempotentAndReturns204()
	{
		var order = await CreateOrderAsync();
		await _client.PostAsync($"/api/orders/{order.Id}/cancel", content: null);

		var response = await _client.PostAsync($"/api/orders/{order.Id}/cancel", content: null);

		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact]
	public async Task CancelOrder_WhenOrderDoesNotExist_Returns404()
	{
		var response = await _client.PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", content: null);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task CancelOrder_WhenAlreadyShipped_Returns409()
	{
		var order = await CreateOrderAsync();
		await SetOrderStatusToShippedAsync(order.Id);

		var response = await _client.PostAsync($"/api/orders/{order.Id}/cancel", content: null);

		Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
	}

	private async Task<CreateProductResponse> CreateProductAsync(string name, decimal price)
	{
		var response = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(name, price));
		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<CreateProductResponse>(TestJson.Options))!;
	}

	private async Task<CreateOrderResponse> CreateOrderAsync()
	{
		var product = await CreateProductAsync("Widget", 10m);
		var request = new CreateOrderRequest(Guid.NewGuid(), [new CreateOrderRequestItem(product.Id, 1)]);
		var response = await _client.PostAsJsonAsync("/api/orders", request);
		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<CreateOrderResponse>(TestJson.Options))!;
	}

	// There's no "ship order" feature yet, so the only way to reach the Shipped
	// state for this test is to set it directly against the database.
	private async Task SetOrderStatusToShippedAsync(Guid orderId)
	{
		using var scope = factory.Services.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		var order = await dbContext.Orders.FirstAsync(o => o.Id == new OrderId(orderId));
		typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Shipped);
		await dbContext.SaveChangesAsync();
	}
}
