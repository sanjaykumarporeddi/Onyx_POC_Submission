using AutoMapper;
using Onyx.Services.ProductAPI.Models;
using Onyx.Services.ProductAPI.Models.Dto;

namespace Onyx.Services.ProductAPI
{
    public static class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<ProductDto, Product>();
                config.CreateMap<Product, ProductDto>();
            });
            return mappingConfig;
        }
    }
}