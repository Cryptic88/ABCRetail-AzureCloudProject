
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace StoreToTableFunction
{
    public class Order : ITableEntity
    {
        public string? Order_ID { get; set; }
        public string? Customer_Id { get; set; } // FK to Customer.RowKey
        public string? Product_ID { get; set; }  // FK to Product.RowKey
        public int Quantity { get; set; }
        public string? Status { get; set; } = "Pending"; // Pending, Processed, Completed
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public string? PartitionKey { get; set; } = string.Empty;
        public string? RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        
        [JsonIgnore]
        public ETag ETag { get; set; }
    }
}
