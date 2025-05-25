using System.ComponentModel.DataAnnotations;

namespace Onyx.Services.ProductAPI.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Product description cannot be empty.")]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Colour { get; set; }
    }
}