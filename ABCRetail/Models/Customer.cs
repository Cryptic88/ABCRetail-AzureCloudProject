using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Models
{
    public class Customer : ITableEntity
    {
        public int Customer_Id { get; set; }
        public string? Customer_Name { get; set; }
        public string? email { get; set; }
        public string? password { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
