using ABCRetail.Models;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;

        public TableStorageService(string connectionString)
        {
            _customerTableClient = new TableClient(connectionString, "customer");
            _productTableClient = new TableClient(connectionString, "product");

        }
        public async Task<List<Customer>> GetAllCustomerAsync()
        {
            var birders = new List<Customer>();
            await foreach (var customer in _customerTableClient.QueryAsync<Customer>())
            {
                birders.Add(customer);
            }
            return birders;
        }
        public async Task AddCustomerAsync(Customer customer)
        {
            if (string.IsNullOrEmpty(customer.PartitionKey) || string.IsNullOrEmpty(customer.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set for the entity.");
            }

            try
            {
                await _customerTableClient.AddEntityAsync(customer);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Failed to add the birder. Ensure that PartitionKey and RowKey are unique and valid.", ex);
            }
        }

        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();
            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }
            return products;
        }

        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set");
            }

            try
            {
                await _productTableClient.AddEntityAsync(product);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding entity to table storage, ex");
            }
        }

        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
