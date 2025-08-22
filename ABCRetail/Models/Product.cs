using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Product : ITableEntity
    {
        [Key]
        public string? Product_ID { get; set; }
        public string? Product_Name { get; set; }
        public string? Description { get; set; }
        public string? ImageURL { get; set; }
        public int? Price { get; set; }

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
