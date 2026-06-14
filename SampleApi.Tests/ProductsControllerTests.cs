<<<<<<< Updated upstream
using System;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SampleApi.DTOs;
using Xunit;
=======
using Xunit;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleApi;
>>>>>>> Stashed changes

namespace SampleApi.Controllers.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkObject_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.GetAll();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
<<<<<<< Updated upstream
    public async Task GetAll_ReturnsNotFound_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.GetAll();

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOkObject_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.GetById(1);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.GetById(-1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenSuccessful()
=======
    public async Task GetAll_Returns200_WithSystem_Collections_Generic_IEnumerable_SampleApi_Models_Product()
    {
        var response = await _client.GetAsync("api/products");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<System.Collections.Generic.IEnumerable<SampleApi.Models.Product>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetById_Returns200_WithSampleApi_Models_Product()
    {
        var response = await _client.GetAsync("api/products/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SampleApi.Models.Product>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetDetails_Returns200_WithSampleApi_DTOs_ProductDetailsDto()
    {
        var response = await _client.GetAsync("api/products/1/details");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SampleApi.DTOs.ProductDetailsDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Create_Returns201_WithSampleApi_Models_Product()
>>>>>>> Stashed changes
    {
        // Arrange
        var mockService = new Mock<IProductService>();

<<<<<<< Updated upstream
        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0M, IsActive = true });

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
=======
        var response = await _authClient.PostAsJsonAsync("api/products", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SampleApi.Models.Product>();
        Assert.NotNull(result);
>>>>>>> Stashed changes
    }

    [Fact]
    public async Task Create_ReturnsBadRequestObject_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0M, IsActive = true });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
<<<<<<< Updated upstream
    public async Task Create_Returns401_WhenUnauthorized()
=======
    public async Task Update_Returns200_WithSampleApi_Models_Product()
>>>>>>> Stashed changes
    {
        // Arrange
        var mockService = new Mock<IProductService>();

<<<<<<< Updated upstream
        var controller = new ProductsController(mockService.Object);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0M, IsActive = true });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
=======
        var response = await _authClient.PutAsJsonAsync("api/products/1", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SampleApi.Models.Product>();
        Assert.NotNull(result);
>>>>>>> Stashed changes
    }

    [Fact]
    public async Task Update_ReturnsOkObject_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Update(1, new UpdateProductDto { Name = "test", Price = 1.0M });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
<<<<<<< Updated upstream
    public async Task Update_ReturnsBadRequestObject_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Update(-1, new UpdateProductDto { Name = "test", Price = 1.0M });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
=======
    public async Task Delete_Returns204()
    {
        var response = await _authClient.DeleteAsync("api/products/1");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
>>>>>>> Stashed changes
    }

    [Fact]
    public async Task Update_Returns401_WhenUnauthorized()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var result = await controller.Update(1, new UpdateProductDto { Name = "test", Price = 1.0M });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Delete(-1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Returns401_WhenUnauthorized()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var result = await controller.Delete(1);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

}
