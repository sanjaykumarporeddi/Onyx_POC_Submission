using AutoMapper;
using Onyx.Services.ProductAPI.Data;
using Onyx.Services.ProductAPI.Models;
using Onyx.Services.ProductAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Onyx.Services.ProductAPI.Services;
using Onyx.Services.ProductAPI.Events;
using Onyx.Services.ProductAPI.Common;

namespace Onyx.Services.ProductAPI.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;
        private readonly ILogger<ProductRepository> _logger;
        private readonly bool _isInMemoryProvider;
        private static readonly string InMemoryProviderName = "Microsoft.EntityFrameworkCore.InMemory";
        private readonly IEventPublisher _eventPublisher;

        public ProductRepository(
            AppDbContext db,
            IMapper mapper,
            ILogger<ProductRepository> logger,
            IEventPublisher eventPublisher)
        {
            _db = db;
            _mapper = mapper;
            _logger = logger;
            _eventPublisher = eventPublisher;
            _isInMemoryProvider = db.Database.ProviderName == InMemoryProviderName;
        }

        public async Task<List<ProductDto>> GetAllProductsAsync(ProductQueryParameters queryParams)
        {
            _logger.LogInformation(AppConstants.LogMessages.RepoFetchingProducts, queryParams.Colour);
            IQueryable<Product> query = _db.Products.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(queryParams.Colour))
            {
                query = _isInMemoryProvider
                    ? query.Where(p => p.Colour != null && p.Colour.ToLowerInvariant().Contains(queryParams.Colour.ToLowerInvariant()))
                    : query.Where(p => p.Colour != null && EF.Functions.ILike(p.Colour, $"%{queryParams.Colour}%"));
            }
            query = query.OrderBy(p => p.ProductId);
            var products = await query.ToListAsync();
            var productDtos = _mapper.Map<List<ProductDto>>(products);
            _logger.LogInformation(AppConstants.LogMessages.RepoFetchedProducts, productDtos.Count);
            return productDtos;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            _logger.LogInformation(AppConstants.LogMessages.RepoFetchingProductById, id);
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) _logger.LogWarning(AppConstants.LogMessages.RepoProductNotFoundById, id);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto?> GetProductByNameAsync(string name)
        {
            _logger.LogInformation(AppConstants.LogMessages.RepoFetchingProductByName, name);
            Product? product;
            var queryable = _db.Products.AsNoTracking();
            if (_isInMemoryProvider) { var nameLower = name.ToLowerInvariant(); product = await queryable.FirstOrDefaultAsync(p => p.Name.ToLowerInvariant() == nameLower); }
            else { product = await queryable.FirstOrDefaultAsync(p => EF.Functions.ILike(p.Name, name)); }
            if (product == null) _logger.LogWarning(AppConstants.LogMessages.RepoProductNotFoundByName, name);
            return _mapper.Map<ProductDto>(product);
        }
        public async Task<ProductDto?> CreateProductAsync(ProductDto productDto)
        {
            _logger.LogInformation(AppConstants.LogMessages.RepoCreatingProduct, productDto.Name);
            var product = _mapper.Map<Product>(productDto);
            await _db.Products.AddAsync(product);
            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation(AppConstants.LogMessages.RepoProductCreated, product.ProductId, product.Name);
                var createdEvent = new ProductChangedEvent(product.ProductId, product.Name, product.Price, ProductChangeType.Created);
                await _eventPublisher.PublishProductChangedEventAsync(createdEvent);
                return _mapper.Map<ProductDto>(product);
            }
            catch (DbUpdateException ex) { _logger.LogError(ex, AppConstants.LogMessages.RepoErrorCreatingProduct, productDto.Name); return null; }
        }
        public async Task<ProductDto?> UpdateProductAsync(ProductDto productDto)
        {
            _logger.LogInformation(AppConstants.LogMessages.RepoUpdatingProduct, productDto.ProductId);
            var existingProduct = await _db.Products.FindAsync(productDto.ProductId);
            if (existingProduct == null) { _logger.LogWarning(AppConstants.LogMessages.RepoProductUpdateNotFound, productDto.ProductId); return null; }
            _mapper.Map(productDto, existingProduct);
            try
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation(AppConstants.LogMessages.RepoProductUpdated, existingProduct.ProductId, existingProduct.Name);
                var updatedEvent = new ProductChangedEvent(existingProduct.ProductId, existingProduct.Name, existingProduct.Price, ProductChangeType.Updated);
                await _eventPublisher.PublishProductChangedEventAsync(updatedEvent);
                return _mapper.Map<ProductDto>(existingProduct);
            }
            catch (DbUpdateException ex) { _logger.LogError(ex, AppConstants.LogMessages.RepoErrorUpdatingProduct, productDto.ProductId); return null; }
        }
        public async Task<bool> DeleteProductAsync(int id)
        {
            _logger.LogInformation(AppConstants.LogMessages.RepoDeletingProduct, id);
            var product = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) { _logger.LogWarning(AppConstants.LogMessages.RepoProductDeleteNotFound, id); return false; }
            var productDetailsForEvent = new { product.ProductId, product.Name, product.Price };
            _db.Products.Remove(product);
            try
            {
                var changes = await _db.SaveChangesAsync();
                if (changes > 0)
                {
                    _logger.LogInformation(AppConstants.LogMessages.RepoProductDeleted, id);
                    var deletedEvent = new ProductChangedEvent(productDetailsForEvent.ProductId, productDetailsForEvent.Name, productDetailsForEvent.Price, ProductChangeType.Deleted);
                    await _eventPublisher.PublishProductChangedEventAsync(deletedEvent);
                    return true;
                }
                _logger.LogWarning(AppConstants.LogMessages.RepoProductDeleteZeroChanges, id); return false;
            }
            catch (DbUpdateException ex) { _logger.LogError(ex, AppConstants.LogMessages.RepoErrorDeletingProduct, id); return false; }
        }
    }
}