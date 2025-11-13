using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class OrderItemSql
    {
        [Key]
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }
        public OrderSql Order { get; set; }

        public string? ProductId { get; set; }
        public ProductSql Product { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
