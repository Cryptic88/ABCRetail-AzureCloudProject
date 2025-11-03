using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class OrderSql
    {
        [Key]
        public int OrderId { get; set; }

        public int CustomerId { get; set; }
        public CustomerSql Customer { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItemSql> OrderItems { get; set; }
    }
}
