using System.ComponentModel.DataAnnotations;

namespace ABCRetail.Models
{
    public class CustomerSql
    {
        [Key]
        public int CustomerId { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }

        public ICollection<OrderSql>? Orders { get; set; }
    }
}
