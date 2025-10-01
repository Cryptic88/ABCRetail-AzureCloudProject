using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Product : ITableEntity
    {
        [Key]
        [JsonPropertyName("productID")]
        public string? Product_ID { get; set; }
        [JsonPropertyName("productName")]
        public string? Product_Name { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("imageURL")]
        public string? ImageURL { get; set; }
        [JsonPropertyName("price")]
        public int? Price { get; set; }

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        [JsonIgnore]
        public ETag ETag { get; set; }
    }
}
