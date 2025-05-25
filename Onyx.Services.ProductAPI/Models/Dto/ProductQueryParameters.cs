using Microsoft.AspNetCore.Mvc;

namespace Onyx.Services.ProductAPI.Models.Dto
{
    public class ProductQueryParameters
    {
        [FromQuery(Name = "colour")]
        public string? Colour { get; set; }
    }
}