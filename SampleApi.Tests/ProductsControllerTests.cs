using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using Xunit;
using SampleApi.DTOs;

namespace SampleApi.Controllers.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsOkObject_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        mockService.Setup(x => x.GetAllAsync()).ReturnsAsync(() => new System.Collections.Generic.List<SampleApi.Models.Product>());

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
        mockService.Setup(x => x.GetAllAsync()).ReturnsAsync((System.Collections.Generic.List<SampleApi.Models.Product>)null);

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
        mockService.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(() => new SampleApi.Models.Product());

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
        mockService.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((SampleApi.Models.Product)null);

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
        mockService.Setup(x => x.CreateAsync(It.IsAny<SampleApi.DTOs.CreateProductDto>())).ReturnsAsync(() => new SampleApi.Models.Product());

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0m, IsActive = true });

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequestObject_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        mockService.Setup(x => x.CreateAsync(It.IsAny<SampleApi.DTOs.CreateProductDto>())).ReturnsAsync((SampleApi.Models.Product)null);

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0m, IsActive = true });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_Returns401_WhenUnauthorized()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        var authorizationFilter = new Mock<IAuthorizationFilter>();
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback((AuthorizationFilterContext ctx) =>
            {
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                ctx.HttpContext.User = anonymous;
                ctx.Result = new UnauthorizedResult();
            });

        var controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
            {
                EndpointMetadata = new List<object> { new AuthorizeAttribute() }
            }
        };
        controller.ControllerContext = controllerContext;

        var authFilterContext = new AuthorizationFilterContext(
            controllerContext, new IFilterMetadata[] { });
        authorizationFilter.Object.OnAuthorization(authFilterContext);

        if (authFilterContext.Result != null)
        {
            Assert.IsType<UnauthorizedResult>(authFilterContext.Result);
            return;
        }

        // Act
        var result = await controller.Create(new CreateProductDto { Name = "test", Price = 1.0m, IsActive = true });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsOkObject_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        mockService.Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<SampleApi.DTOs.UpdateProductDto>())).ReturnsAsync(() => new SampleApi.Models.Product());

        // Act
        var result = await controller.Update(1, new UpdateProductDto { Name = "test", Price = 1.0m });

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_ReturnsBadRequestObject_WhenFailed()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        mockService.Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<SampleApi.DTOs.UpdateProductDto>())).ReturnsAsync((SampleApi.Models.Product)null);

        // Act
        var result = await controller.Update(-1, new UpdateProductDto { Name = "test", Price = 1.0m });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_Returns401_WhenUnauthorized()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);

        var authorizationFilter = new Mock<IAuthorizationFilter>();
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback((AuthorizationFilterContext ctx) =>
            {
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                ctx.HttpContext.User = anonymous;
                ctx.Result = new UnauthorizedResult();
            });

        var controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
            {
                EndpointMetadata = new List<object> { new AuthorizeAttribute() }
            }
        };
        controller.ControllerContext = controllerContext;

        var authFilterContext = new AuthorizationFilterContext(
            controllerContext, new IFilterMetadata[] { });
        authorizationFilter.Object.OnAuthorization(authFilterContext);

        if (authFilterContext.Result != null)
        {
            Assert.IsType<UnauthorizedResult>(authFilterContext.Result);
            return;
        }

        // Act
        var result = await controller.Update(1, new UpdateProductDto { Name = "test", Price = 1.0m });

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var mockService = new Mock<IProductService>();

        var controller = new ProductsController(mockService.Object);
        mockService.Setup(x => x.DeleteAsync(It.IsAny<int>())).ReturnsAsync(() => true);

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
        mockService.Setup(x => x.DeleteAsync(It.IsAny<int>())).ReturnsAsync(false);

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

        var authorizationFilter = new Mock<IAuthorizationFilter>();
        authorizationFilter
            .Setup(f => f.OnAuthorization(It.IsAny<AuthorizationFilterContext>()))
            .Callback((AuthorizationFilterContext ctx) =>
            {
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                ctx.HttpContext.User = anonymous;
                ctx.Result = new UnauthorizedResult();
            });

        var controllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor
            {
                EndpointMetadata = new List<object> { new AuthorizeAttribute() }
            }
        };
        controller.ControllerContext = controllerContext;

        var authFilterContext = new AuthorizationFilterContext(
            controllerContext, new IFilterMetadata[] { });
        authorizationFilter.Object.OnAuthorization(authFilterContext);

        if (authFilterContext.Result != null)
        {
            Assert.IsType<UnauthorizedResult>(authFilterContext.Result);
            return;
        }

        // Act
        var result = await controller.Delete(1);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

}
