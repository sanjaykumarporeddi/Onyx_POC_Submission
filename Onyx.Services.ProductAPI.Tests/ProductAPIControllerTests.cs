using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Onyx.Services.ProductAPI.Controllers;
using Onyx.Services.ProductAPI.Models.Dto;
using Onyx.Services.ProductAPI.Repository;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Onyx.Services.ProductAPI.Common;
using Onyx.Common.Shared.Dtos;
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // Required for IObjectModelValidator

namespace Onyx.Services.ProductAPI.Tests.Controllers
{
    /// <summary>
    /// Unit tests for ProductAPIController with mocked dependencies.
    /// Follows Arrange-Act-Assert pattern.
    /// </summary>
    public class ProductAPIControllerTests
    {
        private readonly Mock<IProductRepository> _mockRepo;
        private readonly Mock<ILogger<ProductAPIController>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockMemoryCache;
        private readonly ProductAPIController _controller;
        private readonly Mock<ICacheEntry> _mockCacheEntry;
        private readonly Mock<IObjectModelValidator> _mockObjectModelValidator; // For TryValidateModel

        public ProductAPIControllerTests()
        {
            _mockRepo = new Mock<IProductRepository>();
            _mockLogger = new Mock<ILogger<ProductAPIController>>();
            _mockMemoryCache = new Mock<IMemoryCache>();
            _mockCacheEntry = new Mock<ICacheEntry>();

            _controller = new ProductAPIController(_mockRepo.Object, _mockLogger.Object, _mockMemoryCache.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            _mockMemoryCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(_mockCacheEntry.Object);

            // Setup for TryValidateModel
            _mockObjectModelValidator = new Mock<IObjectModelValidator>();
            _mockObjectModelValidator.Setup(o => o.Validate(It.IsAny<ActionContext>(),
                                                          It.IsAny<ValidationStateDictionary>(),
                                                          It.IsAny<string>(),
                                                          It.IsAny<object>()));
            _controller.ObjectValidator = _mockObjectModelValidator.Object;
        }

        [Fact]
        public async Task GetAllProducts_ReturnsOkWithListOfProducts()
        {
            // Arrange
            var queryParameters = new ProductQueryParameters();
            var productItems = new List<ProductDto> { new ProductDto { ProductId = 1, Name = "Test Product" } };
            _mockRepo.Setup(repo => repo.GetAllProductsAsync(queryParameters)).ReturnsAsync(productItems);

            // Act
            var actionResult = await _controller.GetAllProducts(queryParameters);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<List<ProductDto>>>(okResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.Single(responseDto.Result);
            Assert.Equal("Test Product", responseDto.Result.First().Name);
        }

        [Fact]
        public async Task GetProductById_CacheHit_ReturnsProductFromCache()
        {
            // Arrange
            var productId = 1;
            var cachedProduct = new ProductDto { ProductId = productId, Name = "Cached Product" };
            object? outValue = cachedProduct;
            _mockMemoryCache.Setup(cache => cache.TryGetValue($"{AppConstants.CacheKeys.ProductPrefix}{productId}", out outValue)).Returns(true);

            // Act
            var actionResult = await _controller.GetProductById(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<ProductDto>>(okResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.Equal(cachedProduct, responseDto.Result);
            _mockRepo.Verify(repo => repo.GetProductByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetProductById_CacheMiss_FetchesFromRepositoryAndCaches()
        {
            // Arrange
            var productId = 1;
            var productFromRepo = new ProductDto { ProductId = productId, Name = "Repo Product" };
            object? outValueNull = null;
            _mockMemoryCache.Setup(cache => cache.TryGetValue($"{AppConstants.CacheKeys.ProductPrefix}{productId}", out outValueNull)).Returns(false);
            _mockRepo.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(productFromRepo);

            // Act
            var actionResult = await _controller.GetProductById(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<ProductDto>>(okResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.Equal(productFromRepo, responseDto.Result);
            _mockRepo.Verify(repo => repo.GetProductByIdAsync(productId), Times.Once);
            _mockMemoryCache.Verify(m => m.CreateEntry($"{AppConstants.CacheKeys.ProductPrefix}{productId}"), Times.Once);
        }

        [Fact]
        public async Task GetProductById_NotFoundInRepository_ReturnsNotFoundResult()
        {
            // Arrange
            var productId = 99;
            object? outValueNull = null;
            _mockMemoryCache.Setup(cache => cache.TryGetValue($"{AppConstants.CacheKeys.ProductPrefix}{productId}", out outValueNull)).Returns(false);
            _mockRepo.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync((ProductDto?)null);

            // Act
            var actionResult = await _controller.GetProductById(productId);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetProductByName_ProductExists_ReturnsOkWithProduct()
        {
            // Arrange
            var productName = "Existing Product";
            var productFromRepo = new ProductDto { ProductId = 1, Name = productName };
            _mockRepo.Setup(repo => repo.GetProductByNameAsync(productName)).ReturnsAsync(productFromRepo);

            // Act
            var actionResult = await _controller.GetProductByName(productName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<ProductDto>>(okResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.Equal(productFromRepo, responseDto.Result);
        }

        [Fact]
        public async Task GetProductByName_ProductNotExists_ReturnsNotFound()
        {
            // Arrange
            var productName = "NonExistent Product";
            _mockRepo.Setup(repo => repo.GetProductByNameAsync(productName)).ReturnsAsync((ProductDto?)null);

            // Act
            var actionResult = await _controller.GetProductByName(productName);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task CreateProduct_ValidModel_ReturnsCreatedAtActionWithProduct()
        {
            // Arrange
            var productDto = new ProductDto { Name = "New Product", Price = 10m, CategoryName = "Test", Description = "Desc" };
            var createdDto = new ProductDto { ProductId = 1, Name = productDto.Name, Price = productDto.Price, CategoryName = productDto.CategoryName, Description = productDto.Description };
            _mockRepo.Setup(repo => repo.CreateProductAsync(productDto)).ReturnsAsync(createdDto);

            // Act
            var actionResult = await _controller.CreateProduct(productDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<ProductDto>>(createdAtActionResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.Equal(createdDto, responseDto.Result);
            Assert.Equal(AppConstants.ApiRoutes.Names.GetProductById, createdAtActionResult.ActionName);
            Assert.Equal(createdDto.ProductId, createdAtActionResult.RouteValues?["id"]);
        }

        [Fact]
        public async Task CreateProduct_InvalidModel_ReturnsBadRequestWithModelState()
        {
            // Arrange
            var productDto = new ProductDto();
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var actionResult = await _controller.CreateProduct(productDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            // When returning BadRequest(ModelState), the value is usually a SerializableError or ValidationProblemDetails
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequestResult.Value); // Or ValidationProblemDetails
            Assert.True(errors.ContainsKey("Name"));
        }

        [Fact]
        public async Task CreateProduct_RepositoryFailure_ReturnsProblemDetails()
        {
            // Arrange
            var productDto = new ProductDto { Name = "Valid But Fails", Price = 10m, CategoryName = "Test", Description = "Desc" };
            _mockRepo.Setup(repo => repo.CreateProductAsync(productDto)).ReturnsAsync((ProductDto?)null);

            // Act
            var actionResult = await _controller.CreateProduct(productDto);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
            Assert.Equal(AppConstants.ProblemDetails.Titles.ProductCreationError, problemDetails.Title);
        }

        [Fact]
        public async Task UpdateProduct_ExistingProduct_ReturnsOkWithUpdatedProductAndInvalidatesCache()
        {
            // Arrange
            var productId = 1;
            var productDto = new ProductDto { ProductId = productId, Name = "Updated Product" };
            _mockRepo.Setup(repo => repo.UpdateProductAsync(It.IsAny<ProductDto>())).ReturnsAsync(productDto);

            // Act
            var actionResult = await _controller.UpdateProduct(productId, productDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<ProductDto>>(okResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.Equal(productDto, responseDto.Result);
            _mockMemoryCache.Verify(cache => cache.Remove($"{AppConstants.CacheKeys.ProductPrefix}{productId}"), Times.Once);
        }

        [Fact]
        public async Task UpdateProduct_ProductIdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var urlId = 1;
            var productDto = new ProductDto { ProductId = 2, Name = "Mismatch" };

            // Act
            var actionResult = await _controller.UpdateProduct(urlId, productDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            // When ModelState is explicitly added to and BadRequest(ModelState) is returned,
            // the Value is typically a SerializableError (Dictionary<string, string[]>) or ValidationProblemDetails
            var errors = Assert.IsAssignableFrom<SerializableError>(badRequestResult.Value); // Or ValidationProblemDetails
            Assert.True(errors.ContainsKey(nameof(ProductDto.ProductId)));
            var productIdErrors = Assert.IsAssignableFrom<string[]>(errors[nameof(ProductDto.ProductId)]);
            Assert.Contains(AppConstants.ProblemDetails.DetailFormats.ProductIdMismatch, productIdErrors);
        }

        [Fact]
        public async Task UpdateProduct_ProductNotFound_ReturnsNotFound()
        {
            // Arrange
            var productId = 1;
            var productDto = new ProductDto { ProductId = productId, Name = "NonExistent Update" };
            _mockRepo.Setup(repo => repo.UpdateProductAsync(It.IsAny<ProductDto>())).ReturnsAsync((ProductDto?)null);

            // Act
            var actionResult = await _controller.UpdateProduct(productId, productDto);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task PartiallyUpdateProduct_ValidPatch_ReturnsOkWithPatchedProductAndInvalidatesCache()
        {
            // Arrange
            var productId = 1;
            var existingProduct = new ProductDto { ProductId = productId, Name = "Original Name", Price = 10m, Description = "Original Desc", CategoryName = "Original Cat" };
            var patchDoc = new JsonPatchDocument<ProductDto>();
            patchDoc.Replace(p => p.Name, "Patched Name");

            _mockRepo.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync(existingProduct);
            // Simulate successful update by returning the modified product
            _mockRepo.Setup(repo => repo.UpdateProductAsync(It.Is<ProductDto>(p => p.Name == "Patched Name" && p.ProductId == productId)))
                     .ReturnsAsync((ProductDto dto) => new ProductDto
                     { // Ensure a new Dto instance is returned as repo would
                         ProductId = dto.ProductId,
                         Name = dto.Name,
                         Price = dto.Price, // Price should be original from 'existingProduct'
                         Description = dto.Description, // Description should be original
                         CategoryName = dto.CategoryName, // CategoryName should be original
                         Colour = dto.Colour
                     });

            // Make sure TryValidateModel doesn't throw; it will add errors to ModelState if any occur.
            // For a successful path, we assume it passes or only adds errors that don't prevent the flow.
            _mockObjectModelValidator.Setup(o => o.Validate(
               It.IsAny<ActionContext>(),
               It.IsAny<ValidationStateDictionary>(),
               It.IsAny<string>(),
               It.Is<ProductDto>(p => p.Name == "Patched Name"))) // Validate the object after patch
               .Callback((ActionContext actionContext, ValidationStateDictionary vud, string prefix, object model) =>
               {
                   // Simulate validation: if model is ProductDto and Name is "Patched Name", it's valid for this test.
                   // If you needed to test validation failures after patch, you'd add errors to actionContext.ModelState here.
               });


            // Act
            var actionResult = await _controller.PartiallyUpdateProduct(productId, patchDoc);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var responseDto = Assert.IsType<ResponseDto<ProductDto>>(okResult.Value);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.Equal("Patched Name", responseDto.Result.Name);
            Assert.Equal(existingProduct.Price, responseDto.Result.Price); // Check other props remain
            _mockMemoryCache.Verify(cache => cache.Remove($"{AppConstants.CacheKeys.ProductPrefix}{productId}"), Times.Once);
        }

        [Fact]
        public async Task PartiallyUpdateProduct_NullPatchDoc_ReturnsBadRequest()
        {
            // Arrange
            var productId = 1;
            JsonPatchDocument<ProductDto>? patchDoc = null;

            // Act
            var actionResult = await _controller.PartiallyUpdateProduct(productId, patchDoc!);

            // Assert
            // Controller returns: BadRequest(new ProblemDetails { Title = ..., Status = ..., Instance = ... });
            // This results in an ObjectResult whose Value is ProblemDetails.
            // BadRequestObjectResult is a specific kind of ObjectResult.
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
            var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
            Assert.Equal(AppConstants.ProblemDetails.Titles.PatchDocumentRequired, problemDetails.Title);
        }

        [Fact]
        public async Task PartiallyUpdateProduct_ProductNotFound_ReturnsNotFound()
        {
            // Arrange
            var productId = 1;
            var patchDoc = new JsonPatchDocument<ProductDto>();
            _mockRepo.Setup(repo => repo.GetProductByIdAsync(productId)).ReturnsAsync((ProductDto?)null);

            // Act
            var actionResult = await _controller.PartiallyUpdateProduct(productId, patchDoc);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task DeleteProduct_ExistingProduct_ReturnsNoContentAndInvalidatesCache()
        {
            // Arrange
            var productId = 1;
            _mockRepo.Setup(repo => repo.DeleteProductAsync(productId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockMemoryCache.Verify(cache => cache.Remove($"{AppConstants.CacheKeys.ProductPrefix}{productId}"), Times.Once);
        }

        [Fact]
        public async Task DeleteProduct_ProductNotFound_ReturnsNotFound()
        {
            // Arrange
            var productId = 1;
            _mockRepo.Setup(repo => repo.DeleteProductAsync(productId)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}