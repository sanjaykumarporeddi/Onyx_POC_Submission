using Onyx.Services.ProductAPI.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Onyx.Services.ProductAPI.Repository
{
    public interface IProductRepository
    {
        Task<List<ProductDto>> GetAllProductsAsync(ProductQueryParameters queryParameters);
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<ProductDto?> GetProductByNameAsync(string name);
        Task<ProductDto?> CreateProductAsync(ProductDto productDto);
        Task<ProductDto?> UpdateProductAsync(ProductDto productDto);
        Task<bool> DeleteProductAsync(int id);
    }
}