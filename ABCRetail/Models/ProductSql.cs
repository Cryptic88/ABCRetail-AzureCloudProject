using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class ProductSql
    {
        [Key]
        public int ProductId { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int StockQty { get; set; }

        public ICollection<OrderItemSql> OrderItems { get; set; }
    }
}
