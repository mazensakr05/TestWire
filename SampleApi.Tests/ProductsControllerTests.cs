using Xunit;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleApi;
using SampleApi.DTOs;

namespace TestWire.Generated.Tests;

public class ProductsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Returns200_WithListOfProduct()
    {
        var response = await _client.GetAsync("api/products");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetById_Returns200_WithProduct()
    {
        var response = await _client.GetAsync("api/products/1");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Create_Returns200_WithString()
    {
        var request = new SampleApi.DTOs.CreateProductDto
        {
            Name = "test",
            Price = 1.0m,
            IsActive = true,
        };

        var response = await _client.PostAsJsonAsync("api/products", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<string>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Create_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync("api/products", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns200_WithProduct()
    {
        var request = new SampleApi.DTOs.UpdateProductDto
        {
            Name = "test",
            Price = 1.0m,
        };

        var response = await _client.PutAsJsonAsync("api/products/1", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<Product>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Update_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PutAsJsonAsync("api/products/1", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns200()
    {
        var response = await _client.DeleteAsync("api/products/1");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Delete_Returns401_WhenUnauthenticated()
    {
        var response = await _client.DeleteAsync("api/products/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}
