using Newtonsoft.Json;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Order : ITableEntity
    {
        public string? Order_ID { get; set; }
        public string? Customer_Id { get; set; }
        public string? Product_ID { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; } = "Pending"; // Pending, Processed, Completed
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }

        [JsonIgnore]
        public DateTimeOffset? Timestamp { get; set; }
        
        [JsonIgnore]
        public ETag ETag { get; set; } = ETag.All;
    }
}
