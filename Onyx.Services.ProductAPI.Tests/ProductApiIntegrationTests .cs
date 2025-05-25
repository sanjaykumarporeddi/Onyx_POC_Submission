using Microsoft.AspNetCore.Mvc;
using Onyx.Services.ProductAPI.Models.Dto;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Onyx.Services.ProductAPI.Common;
using System.Text;
using Onyx.Common.Shared.Dtos;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Xunit;

namespace Onyx.Services.ProductAPI.Tests.Integration
{
    /// <summary>
    /// Integration tests for Product API endpoints using a test server.
    /// Follows Arrange-Act-Assert pattern.
    /// </summary>
    public class ProductApiIntegrationTests : ProductApiIntegrationTestBase
    {
        public ProductApiIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory) { }

        [Fact]
        public async Task HealthCheck_ReturnsOkAndHealthyMessage()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, AppConstants.ApiRoutes.Health);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
            Assert.Equal(AppConstants.ContentTypes.PlainTextUtf8, response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetAllProducts_Authorized_ReturnsListOfProductsInResponseDto()
        {
            // Arrange
            var request = CreateAuthorizedRequest(HttpMethod.Get, AppConstants.ApiRoutes.ProductsBase);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<List<ProductDto>>>();
            Assert.NotNull(responseDto);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.True(responseDto.Result.Count >= 10, $"Expected >=10 products, got {responseDto.Result.Count}");
            Assert.False(response.Headers.Contains(AppConstants.HttpHeaders.XPagination));
        }

        [Fact]
        public async Task GetAllProducts_FilterByColour_ReturnsCorrectlyFilteredProducts()
        {
            // Arrange
            string testColour = "Golden Brown";
            var request = CreateAuthorizedRequest(HttpMethod.Get, $"{AppConstants.ApiRoutes.ProductsBase}?colour={Uri.EscapeDataString(testColour)}");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<List<ProductDto>>>();
            Assert.NotNull(responseDto);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.Equal(2, responseDto.Result.Count); // Based on seed data.
            Assert.All(responseDto.Result, p => Assert.Contains(testColour, p.Colour ?? "", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData(1, HttpStatusCode.OK, true)]
        [InlineData(99999, HttpStatusCode.NotFound, false)]
        public async Task GetProductById_VaryingExistence_ReturnsExpectedResponse(int productId, HttpStatusCode expectedStatus, bool expectSuccessDtoBody)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.ApiRoutes.ProductsBase}/{productId}");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(expectedStatus, response.StatusCode);
            if (expectSuccessDtoBody && response.IsSuccessStatusCode)
            {
                var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<ProductDto>>();
                Assert.NotNull(responseDto);
                Assert.True(responseDto.IsSuccess);
                Assert.NotNull(responseDto.Result);
                Assert.Equal(productId, responseDto.Result.ProductId);
            }
        }

        [Fact]
        public async Task CreateProduct_Admin_ValidProduct_ReturnsCreatedWithProductInResponseDto()
        {
            // Arrange
            var newProduct = new ProductDto { Name = $"POC_Create_{Guid.NewGuid()}", Price = 19.99m, CategoryName = "POC_Cat", Description = "POC_Desc", Colour = "POC_Colour" };
            var request = CreateAuthorizedRequest(HttpMethod.Post, AppConstants.ApiRoutes.ProductsBase);
            request.Content = JsonContent.Create(newProduct);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<ProductDto>>();
            Assert.NotNull(responseDto);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.Equal(newProduct.Name, responseDto.Result.Name);
            Assert.Equal("Product created successfully.", responseDto.Message);
            Assert.True(responseDto.Result.ProductId > 0);
        }

        [Fact]
        public async Task CreateProduct_Admin_InvalidProduct_ReturnsBadRequestWithValidationProblemDetails()
        {
            // Arrange
            var invalidProduct = new ProductDto { Name = "", Price = 0, CategoryName = "Test", Description = "" };
            var request = CreateAuthorizedRequest(HttpMethod.Post, AppConstants.ApiRoutes.ProductsBase);
            request.Content = JsonContent.Create(invalidProduct);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.NotNull(problemDetails);
            Assert.True(problemDetails.Errors.ContainsKey(nameof(ProductDto.Name)));
            Assert.True(problemDetails.Errors.ContainsKey(nameof(ProductDto.Price)));
            Assert.True(problemDetails.Errors.ContainsKey(nameof(ProductDto.Description)));
        }

        [Fact]
        public async Task UpdateProduct_Admin_ValidFullUpdate_ReturnsOkWithUpdatedProduct()
        {
            // Arrange
            var createdProduct = await CreateTestProductAsync(new ProductDto { Name = $"POC_UpdateFull_{Guid.NewGuid()}", Price = 10m, CategoryName = "Update", Description = "Initial", Colour = "Red" });
            Assert.NotNull(createdProduct); // Ensure setup product was created
            var updatedProductData = new ProductDto { ProductId = createdProduct.ProductId, Name = "Fully Updated Name", Price = 12.50m, CategoryName = "Updated Category", Description = "Fully updated description", Colour = "Blue" };
            var request = CreateAuthorizedRequest(HttpMethod.Put, $"{AppConstants.ApiRoutes.ProductsBase}/{createdProduct.ProductId}");
            request.Content = JsonContent.Create(updatedProductData);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<ProductDto>>();
            Assert.NotNull(responseDto);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.Equal(updatedProductData.Name, responseDto.Result.Name);
            Assert.Equal(updatedProductData.Price, responseDto.Result.Price);
            Assert.Equal(updatedProductData.Colour, responseDto.Result.Colour);
        }

        [Fact]
        public async Task PartiallyUpdateProduct_Admin_UpdateColour_ReturnsOkWithPatchedProduct()
        {
            // Arrange
            var createdProduct = await CreateTestProductAsync(new ProductDto { Name = $"POC_Patch_{Guid.NewGuid()}", Price = 20m, CategoryName = "Patch", Description = "Original", Colour = "Green" });
            Assert.NotNull(createdProduct); // Ensure setup product was created
            var newColour = "Purple";
            var patchDocPayload = new[] { new { op = "replace", path = "/colour", value = newColour } };
            var patchRequestContent = new StringContent(JsonSerializer.Serialize(patchDocPayload), Encoding.UTF8, AppConstants.ContentTypes.JsonPatch);
            var request = CreateAuthorizedRequest(HttpMethod.Patch, $"{AppConstants.ApiRoutes.ProductsBase}/{createdProduct.ProductId}");
            request.Content = patchRequestContent;

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<ProductDto>>();
            Assert.NotNull(responseDto);
            Assert.True(responseDto.IsSuccess);
            Assert.NotNull(responseDto.Result);
            Assert.Equal(newColour, responseDto.Result.Colour);
            Assert.Equal(createdProduct.Name, responseDto.Result.Name); // Check other props unchanged
        }

        [Fact]
        public async Task DeleteProduct_Admin_ExistingProduct_ReturnsNoContentAndProductIsGone()
        {
            // Arrange
            var createdProduct = await CreateTestProductAsync(new ProductDto { Name = $"POC_Delete_{Guid.NewGuid()}", Price = 5m, CategoryName = "Delete", Description = "To be deleted", Colour = "White" });
            Assert.NotNull(createdProduct); // Ensure setup product was created
            var deleteRequest = CreateAuthorizedRequest(HttpMethod.Delete, $"{AppConstants.ApiRoutes.ProductsBase}/{createdProduct.ProductId}");

            // Act
            var deleteResponse = await Client.SendAsync(deleteRequest);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Arrange (for verification)
            var verifyRequest = new HttpRequestMessage(HttpMethod.Get, $"{AppConstants.ApiRoutes.ProductsBase}/{createdProduct.ProductId}");
            // Act (for verification)
            var verifyResponse = await Client.SendAsync(verifyRequest);
            // Assert (for verification)
            Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
        }
    }
}