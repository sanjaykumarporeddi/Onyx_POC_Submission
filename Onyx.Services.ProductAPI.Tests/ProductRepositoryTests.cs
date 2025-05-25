using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Onyx.Services.ProductAPI.Data;
using Onyx.Services.ProductAPI.Models;
using Onyx.Services.ProductAPI.Models.Dto;
using Onyx.Services.ProductAPI.Repository;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using Onyx.Services.ProductAPI;
using System;
using Onyx.Services.ProductAPI.Services;
using Onyx.Services.ProductAPI.Events;
using Onyx.Services.ProductAPI.Common;
using System.Collections.Generic;

namespace Onyx.Services.ProductAPI.Tests.Repository
{
    /// <summary>
    /// Unit tests for ProductRepository using InMemory DB.
    /// Follows Arrange-Act-Assert pattern.
    /// </summary>
    public class ProductRepositoryTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<ProductRepository>> _mockLogger;
        private readonly Mock<IEventPublisher> _mockEventPublisher;

        public ProductRepositoryTests()
        {
            // Arrange (for all tests)
            var mapperConfiguration = MappingConfig.RegisterMaps();
            _mapper = mapperConfiguration.CreateMapper();
            _mockLogger = new Mock<ILogger<ProductRepository>>();
            _mockEventPublisher = new Mock<IEventPublisher>();
        }

        private AppDbContext GetInMemoryDbContext(string dbName)
        {
            // Arrange (helper)
            var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(databaseName: dbName).Options;
            var dbContext = new AppDbContext(options);
            dbContext.Database.EnsureCreated(); // Applies seed data.
            return dbContext;
        }

        [Fact]
        public async Task GetAllProductsAsync_NoFilter_ReturnsAllSeedProducts()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(GetAllProductsAsync_NoFilter_ReturnsAllSeedProducts));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            var queryParameters = new ProductQueryParameters();

            // Act
            var result = await repository.GetAllProductsAsync(queryParameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Count); // Seed data count.
        }

        [Fact]
        public async Task GetAllProductsAsync_WithColourFilter_ReturnsFilteredProducts()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(GetAllProductsAsync_WithColourFilter_ReturnsFilteredProducts));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            string testColour = "Golden Brown";
            var queryParameters = new ProductQueryParameters { Colour = testColour };

            // Act
            var result = await repository.GetAllProductsAsync(queryParameters);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, item => Assert.Contains(testColour, item.Colour ?? "", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData(1, "Scotch Egg")]
        [InlineData(999, null)]
        public async Task GetProductByIdAsync_VaryingExistence_ReturnsExpectedProductOrNull(int productId, string? expectedName)
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext($"{nameof(GetProductByIdAsync_VaryingExistence_ReturnsExpectedProductOrNull)}_{productId}");
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);

            // Act
            var result = await repository.GetProductByIdAsync(productId);

            // Assert
            if (expectedName != null)
            {
                Assert.NotNull(result);
                Assert.Equal(expectedName, result.Name);
                Assert.Equal(productId, result.ProductId);
            }
            else
            {
                Assert.Null(result);
            }
        }

        [Theory]
        [InlineData("Fish and Chips", 4)]
        [InlineData("NonExistent Product", null)]
        public async Task GetProductByNameAsync_VaryingExistence_ReturnsExpectedProductOrNull(string productName, int? expectedId)
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext($"{nameof(GetProductByNameAsync_VaryingExistence_ReturnsExpectedProductOrNull)}_{productName.Replace(" ", "")}");
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);

            // Act
            var result = await repository.GetProductByNameAsync(productName);

            // Assert
            if (expectedId.HasValue)
            {
                Assert.NotNull(result);
                Assert.Equal(productName, result.Name);
                Assert.Equal(expectedId.Value, result.ProductId);
            }
            else
            {
                Assert.Null(result);
            }
        }

        [Fact]
        public async Task CreateProductAsync_ValidProductDto_AddsProductToDatabaseAndPublishesEvent()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(CreateProductAsync_ValidProductDto_AddsProductToDatabaseAndPublishesEvent));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            var newProductDto = new ProductDto { Name = "POC_Coffee", Price = 5.99m, CategoryName = "POC_Bev", Description = "POC_Desc", Colour = "Black" };

            // Act
            var result = await repository.CreateProductAsync(newProductDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ProductId > 0);
            Assert.Equal(newProductDto.Name, result.Name);
            var productInDb = await dbContext.Products.FindAsync(result.ProductId); // Verify in DB context
            Assert.NotNull(productInDb);
            Assert.Equal(newProductDto.Name, productInDb.Name);
            _mockEventPublisher.Verify(p => p.PublishProductChangedEventAsync(
                It.Is<ProductChangedEvent>(e => e.ProductId == result.ProductId &&
                                                e.ChangeType == ProductChangeType.Created &&
                                                e.Name == newProductDto.Name)), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ExistingProduct_UpdatesProductInDatabaseAndPublishesEvent()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(UpdateProductAsync_ExistingProduct_UpdatesProductInDatabaseAndPublishesEvent));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            var updatedProductDto = new ProductDto { ProductId = 1, Name = "Super Scotch Egg", Price = 17.99m, CategoryName = "Starter Deluxe", Description = "Updated description", Colour = "Golden Deluxe" };

            // Act
            var result = await repository.UpdateProductAsync(updatedProductDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updatedProductDto.Name, result.Name);
            Assert.Equal(updatedProductDto.Price, result.Price);
            var productInDb = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == 1); // Verify in DB context
            Assert.NotNull(productInDb);
            Assert.Equal(updatedProductDto.Name, productInDb.Name);
            Assert.Equal(updatedProductDto.Price, productInDb.Price);
            _mockEventPublisher.Verify(p => p.PublishProductChangedEventAsync(
                It.Is<ProductChangedEvent>(e => e.ProductId == 1 &&
                                                e.ChangeType == ProductChangeType.Updated &&
                                                e.Name == updatedProductDto.Name)), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_NonExistingProduct_ReturnsNull()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(UpdateProductAsync_NonExistingProduct_ReturnsNull));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            var nonExistingProductDto = new ProductDto { ProductId = 999, Name = "Non Existent" };

            // Act
            var result = await repository.UpdateProductAsync(nonExistingProductDto);

            // Assert
            Assert.Null(result);
            _mockEventPublisher.Verify(p => p.PublishProductChangedEventAsync(It.IsAny<ProductChangedEvent>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProductAsync_ExistingProduct_RemovesFromDatabaseAndPublishesEvent()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(DeleteProductAsync_ExistingProduct_RemovesFromDatabaseAndPublishesEvent));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            var productIdToDelete = 1;

            // Act
            var result = await repository.DeleteProductAsync(productIdToDelete);

            // Assert
            Assert.True(result);
            var productInDb = await dbContext.Products.FindAsync(productIdToDelete); // Verify in DB context
            Assert.Null(productInDb);
            _mockEventPublisher.Verify(p => p.PublishProductChangedEventAsync(
                It.Is<ProductChangedEvent>(e => e.ProductId == productIdToDelete &&
                                                e.ChangeType == ProductChangeType.Deleted)), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_NonExistingProduct_ReturnsFalse()
        {
            // Arrange
            await using var dbContext = GetInMemoryDbContext(nameof(DeleteProductAsync_NonExistingProduct_ReturnsFalse));
            var repository = new ProductRepository(dbContext, _mapper, _mockLogger.Object, _mockEventPublisher.Object);
            var productIdToDelete = 999;

            // Act
            var result = await repository.DeleteProductAsync(productIdToDelete);

            // Assert
            Assert.False(result);
            _mockEventPublisher.Verify(p => p.PublishProductChangedEventAsync(It.IsAny<ProductChangedEvent>()), Times.Never);
        }
    }
}