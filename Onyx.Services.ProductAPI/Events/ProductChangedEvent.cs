using System;

namespace Onyx.Services.ProductAPI.Events
{
    public enum ProductChangeType
    {
        Created,
        Updated,
        Deleted
    }

    public class ProductChangedEvent
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public ProductChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; }

        public ProductChangedEvent(int productId, string? name, decimal price, ProductChangeType changeType)
        {
            ProductId = productId;
            Name = name;
            Price = price;
            ChangeType = changeType;
            Timestamp = DateTime.UtcNow;
        }
    }
}