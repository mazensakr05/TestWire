using System;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SampleApi.DTOs;
using Xunit;

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
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0M, IsActive = true });

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
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
    public async Task Create_Returns401_WhenUnauthorized()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        controller.ControllerContext = new ControllerContext();
        controller.ControllerContext.HttpContext = new DefaultHttpContext();

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0M, IsActive = true });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
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
    public async Task Update_ReturnsBadRequestObject_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        // Act
        var result = await controller.Update(-1, new UpdateProductDto { Name = "test", Price = 1.0M });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
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
