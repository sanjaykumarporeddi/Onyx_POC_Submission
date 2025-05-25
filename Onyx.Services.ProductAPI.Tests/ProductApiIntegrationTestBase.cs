using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Onyx.Services.ProductAPI.Models.Dto;
using Onyx.Services.ProductAPI.Common;
using Onyx.Common.Shared.Dtos;
using System.Threading.Tasks;
using System;
using System.Net.Http;

namespace Onyx.Services.ProductAPI.Tests.Integration
{
    /// <summary>
    /// Base for integration tests: HttpClient, JWT token acquisition.
    /// Follows Arrange-Act-Assert pattern implicitly in helper methods.
    /// </summary>
    public abstract class ProductApiIntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
    {
        protected readonly HttpClient Client;
        private string? _adminTokenInternal;

        protected ProductApiIntegrationTestBase(CustomWebApplicationFactory<Program> factory)
        {
            // Arrange (implicit for all tests using this base)
            Client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        protected class TokenResponse { [JsonPropertyName("token")] public string? Token { get; set; } }

        protected async Task<string> GetAndCacheAdminTokenAsync()
        {
            // Arrange
            if (_adminTokenInternal != null) return _adminTokenInternal;

            // Act
            var response = await Client.GetAsync(AppConstants.ApiRoutes.IntegrationTestTokenAdmin);

            // Assert
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to generate admin token. Status: {response.StatusCode}. Response: {errorContent}");
            }
            var tokenContainer = await response.Content.ReadFromJsonAsync<TokenResponse>();
            _adminTokenInternal = tokenContainer?.Token ?? throw new InvalidOperationException("Admin token received was null or empty.");
            return _adminTokenInternal;
        }

        protected HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
        {
            // Arrange
            if (_adminTokenInternal == null) throw new InvalidOperationException("Admin token not initialized.");

            // Act
            var request = new HttpRequestMessage(method, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue(AppConstants.Swagger.AuthScheme, _adminTokenInternal);

            // Assert (implicitly by returning the request)
            return request;
        }

        public async Task InitializeAsync()
        {
            // Arrange & Act (for test class setup)
            await GetAndCacheAdminTokenAsync();
        }
        public Task DisposeAsync() => Task.CompletedTask; // No specific async teardown needed

        protected async Task<ProductDto?> CreateTestProductAsync(ProductDto productToCreate)
        {
            // Arrange
            var request = CreateAuthorizedRequest(HttpMethod.Post, AppConstants.ApiRoutes.ProductsBase);
            request.Content = JsonContent.Create(productToCreate);

            // Act
            var response = await Client.SendAsync(request);

            // Assert (partial, for helper success)
            if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var responseDto = await response.Content.ReadFromJsonAsync<ResponseDto<ProductDto>>();
                return responseDto?.Result;
            }
            System.Diagnostics.Debug.WriteLine($"Failed to create test product: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
        }
    }
}