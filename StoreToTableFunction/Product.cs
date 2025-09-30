using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace StoreToTableFunction
{
    public class Product : ITableEntity
    {
        [Key]
        public string? Product_ID { get; set; }
        public string? Product_Name { get; set; }
        public string? Description { get; set; }
        public string? ImageURL { get; set; }
        public int? Price { get; set; }

        public string? PartitionKey { get; set; } = string.Empty;
        public string? RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public ETag ETag { get; set; }
    }
}
