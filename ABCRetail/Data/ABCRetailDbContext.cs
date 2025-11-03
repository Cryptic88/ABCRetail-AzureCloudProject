using Microsoft.EntityFrameworkCore;
using ABCRetail.Models;

namespace ABCRetail.Data
{
    public class ABCRetailDbContext : DbContext
    {
        public ABCRetailDbContext(DbContextOptions<ABCRetailDbContext> options) : base(options)
        {
        }

        public DbSet<CustomerSql> Customer { get; set; }
        public DbSet<ProductSql> Product { get; set; }
        public DbSet<OrderSql> Order { get; set; }
        public DbSet<OrderItemSql> OrderItem { get; set; }
    }
}
