using Onyx.Services.ProductAPI.Models.Dto;
using Onyx.Services.ProductAPI.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Onyx.Services.ProductAPI.Common;    // For AppConstants (which has nested LogMessages, ProblemDetails etc.)
using Onyx.Common.Shared.Dtos;          // For ResponseDto<T>
using Onyx.Common.Shared.Enums;         // For ApplicationRole
using Microsoft.Extensions.Caching.Memory;
using System;
using Microsoft.AspNetCore.JsonPatch;
using System.Collections.Generic;

namespace Onyx.Services.ProductAPI.Controllers
{
    [Route(AppConstants.ApiRoutes.ProductsBase)]
    [ApiController]
    public class ProductAPIController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<ProductAPIController> _logger;
        private readonly IMemoryCache _memoryCache;

        public ProductAPIController(
            IProductRepository productRepository,
            ILogger<ProductAPIController> logger,
            IMemoryCache memoryCache)
        {
            _productRepository = productRepository;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        [HttpGet(Name = AppConstants.ApiRoutes.Names.GetAllProducts)]
        [Authorize]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedRead)]
        public async Task<ActionResult<ResponseDto<List<ProductDto>>>> GetAllProducts([FromQuery] ProductQueryParameters queryParameters)
        {
            _logger.LogInformation(AppConstants.LogMessages.AttemptingGetAllProducts, JsonSerializer.Serialize(queryParameters));
            var products = await _productRepository.GetAllProductsAsync(queryParameters);
            return Ok(ResponseDto<List<ProductDto>>.Success(products));
        }

        [HttpGet("{id:int}", Name = AppConstants.ApiRoutes.Names.GetProductById)]
        [AllowAnonymous]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedRead)]
        public async Task<ActionResult<ResponseDto<ProductDto>>> GetProductById(int id)
        {
            _logger.LogInformation(AppConstants.LogMessages.AttemptingGetProductById, id);
            string cacheKey = $"{AppConstants.CacheKeys.ProductPrefix}{id}";
            if (_memoryCache.TryGetValue(cacheKey, out ProductDto? cachedProduct) && cachedProduct != null)
            {
                _logger.LogInformation(AppConstants.LogMessages.ProductFoundInCache, id); return Ok(ResponseDto<ProductDto>.Success(cachedProduct));
            }
            _logger.LogInformation(AppConstants.LogMessages.ProductNotInCacheFetching, id);
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning(AppConstants.LogMessages.ControllerProductNotFoundById, id);
                return NotFound();
            }
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)).SetAbsoluteExpiration(TimeSpan.FromHours(1)).SetPriority(CacheItemPriority.Normal).SetSize(1);
            _memoryCache.Set(cacheKey, product, cacheEntryOptions);
            _logger.LogInformation(AppConstants.LogMessages.ProductFetchedAndCached, id);
            return Ok(ResponseDto<ProductDto>.Success(product));
        }

        [HttpGet("byName/{productName}", Name = AppConstants.ApiRoutes.Names.GetProductByName)]
        [AllowAnonymous]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedRead)]
        public async Task<ActionResult<ResponseDto<ProductDto>>> GetProductByName(string productName)
        {
            _logger.LogInformation(AppConstants.LogMessages.AttemptingGetProductByName, productName);
            var product = await _productRepository.GetProductByNameAsync(productName);
            if (product == null)
            {
                _logger.LogWarning(AppConstants.LogMessages.ControllerProductNotFoundByName, productName);
                return NotFound();
            }
            return Ok(ResponseDto<ProductDto>.Success(product));
        }

        [HttpPost(Name = AppConstants.ApiRoutes.Names.CreateProduct)]
        [Authorize(Roles = nameof(ApplicationRole.Admin))]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedWrite)]
        public async Task<ActionResult<ResponseDto<ProductDto>>> CreateProduct([FromBody] ProductDto productDto)
        {
            _logger.LogInformation(AppConstants.LogMessages.AttemptingCreateProduct, productDto.Name);
            if (!ModelState.IsValid) { return BadRequest(ModelState); }
            var createdProduct = await _productRepository.CreateProductAsync(productDto);
            if (createdProduct == null)
            {
                _logger.LogError(AppConstants.LogMessages.ControllerProductCreateFailed, productDto.Name);
                var problemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = AppConstants.ProblemDetails.Titles.ProductCreationError, Detail = AppConstants.ProblemDetails.DetailFormats.GenericCreateError, Instance = HttpContext.Request.Path };
                return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
            }
            return CreatedAtAction(AppConstants.ApiRoutes.Names.GetProductById,
                                   new { id = createdProduct.ProductId },
                                   ResponseDto<ProductDto>.Success(createdProduct, "Product created successfully."));
        }

        [HttpPut("{id:int}", Name = AppConstants.ApiRoutes.Names.UpdateProduct)]
        [Authorize(Roles = nameof(ApplicationRole.Admin))]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedWrite)]
        public async Task<ActionResult<ResponseDto<ProductDto>>> UpdateProduct(int id, [FromBody] ProductDto productDto)
        {
            _logger.LogInformation(AppConstants.LogMessages.AttemptingUpdateProduct, id);
            if (id != productDto.ProductId && productDto.ProductId != 0) { ModelState.AddModelError(nameof(productDto.ProductId), AppConstants.ProblemDetails.DetailFormats.ProductIdMismatch); return BadRequest(ModelState); }
            if (productDto.ProductId == 0) productDto.ProductId = id;
            if (!ModelState.IsValid) { return BadRequest(ModelState); }
            var updatedProduct = await _productRepository.UpdateProductAsync(productDto);
            if (updatedProduct == null) { _logger.LogWarning(AppConstants.LogMessages.ControllerProductUpdateNotFound, productDto.ProductId); return NotFound(); }
            string cacheKey = $"{AppConstants.CacheKeys.ProductPrefix}{updatedProduct.ProductId}";
            _memoryCache.Remove(cacheKey); _logger.LogInformation(AppConstants.LogMessages.CacheInvalidatedForProduct, updatedProduct.ProductId, "update"); return Ok(ResponseDto<ProductDto>.Success(updatedProduct, "Product updated successfully."));
        }

        [HttpPatch("{id:int}", Name = AppConstants.ApiRoutes.Names.PartiallyUpdateProduct)]
        [Authorize(Roles = nameof(ApplicationRole.Admin))]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedWrite)]
        public async Task<ActionResult<ResponseDto<ProductDto>>> PartiallyUpdateProduct(int id, [FromBody] JsonPatchDocument<ProductDto> patchDoc)
        {
            if (patchDoc == null) { return BadRequest(new ProblemDetails { Title = AppConstants.ProblemDetails.Titles.PatchDocumentRequired, Status = StatusCodes.Status400BadRequest, Instance = HttpContext.Request.Path }); }
            _logger.LogInformation(AppConstants.LogMessages.AttemptingPatchProduct, id);
            var productToUpdate = await _productRepository.GetProductByIdAsync(id);
            if (productToUpdate == null) { return NotFound(); }
            patchDoc.ApplyTo(productToUpdate, error => { ModelState.AddModelError(string.Empty, error.ErrorMessage); });
            if (!TryValidateModel(productToUpdate)) { return BadRequest(ModelState); }
            var updatedProduct = await _productRepository.UpdateProductAsync(productToUpdate);
            if (updatedProduct == null)
            {
                _logger.LogError(AppConstants.LogMessages.ControllerProductPatchFailed, id);
                var problemDetails = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = AppConstants.ProblemDetails.Titles.PatchUpdateError, Detail = AppConstants.ProblemDetails.DetailFormats.GenericPatchError, Instance = HttpContext.Request.Path };
                return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
            }
            string cacheKey = $"{AppConstants.CacheKeys.ProductPrefix}{updatedProduct.ProductId}";
            _memoryCache.Remove(cacheKey); _logger.LogInformation(AppConstants.LogMessages.CacheInvalidatedForProduct, updatedProduct.ProductId, "patch update");
            return Ok(ResponseDto<ProductDto>.Success(updatedProduct, "Product patched successfully."));
        }

        [HttpDelete("{id:int}", Name = AppConstants.ApiRoutes.Names.DeleteProduct)]
        [Authorize(Roles = nameof(ApplicationRole.Admin))]
        [EnableRateLimiting(AppConstants.RateLimitPolicies.FixedWrite)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            _logger.LogInformation(AppConstants.LogMessages.AttemptingDeleteProduct, id);
            var isDeleted = await _productRepository.DeleteProductAsync(id);
            if (!isDeleted) { _logger.LogWarning(AppConstants.LogMessages.ControllerProductDeleteFailed, id); return NotFound(); }
            string cacheKey = $"{AppConstants.CacheKeys.ProductPrefix}{id}";
            _memoryCache.Remove(cacheKey); _logger.LogInformation(AppConstants.LogMessages.CacheInvalidatedForProduct, id, "deletion");
            return NoContent();
        }
    }
}