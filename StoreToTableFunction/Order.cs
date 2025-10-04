using System;
using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;

namespace StoreToTableFunction
{
    public class Order : ITableEntity
    {
        [JsonPropertyName("Order_ID")]
        public string? Order_ID { get; set; }

        [JsonPropertyName("Customer_Id")]
        public string? Customer_Id { get; set; }

        [JsonPropertyName("Product_ID")]
        public string? Product_ID { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        public string? Status { get; set; } = "Pending";

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public ETag ETag { get; set; } = ETag.All;
    }
}

