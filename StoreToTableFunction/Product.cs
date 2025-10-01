using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace StoreToTableFunction
{
    public class Product : ITableEntity
    {

        [JsonPropertyName("productId")]
        public string Product_ID { get; set; } = string.Empty;
        [JsonPropertyName("productName")]
        public string Product_Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageURL { get; set; } = string.Empty;
        public int Price { get; set; }

        public string PartitionKey { get; set; } = "ProductsPartition";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
