using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleApi;
using SampleApi.DTOs;
using SampleApi.Models;

namespace SampleApi.Tests;

public class CategoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _authClient;

    public CategoriesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Test", "testwire");
    }

    [Fact]
    public async Task Get_Returns200_WithIEnumerable_CategoryDto()
    {
        var response = await _client.GetAsync("api/categories");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<CategoryDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Get_Returns404_WithCategoryDto()
    {
        var response = await _client.GetAsync("api/categories/1");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Post_Returns201_WithCategoryDto()
    {
        var request = new SampleApi.DTOs.CreateCategoryDto
        {
            Name = "test",
            Description = null,
        };

        var response = await _client.PostAsJsonAsync("api/categories", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(result);
    }

}
