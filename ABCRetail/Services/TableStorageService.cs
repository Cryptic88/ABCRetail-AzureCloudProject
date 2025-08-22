using ABCRetail.Models;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Services
{
    public class TableStorageService
    {
        private readonly TableClient _customerTableClient;
        private readonly TableClient _productTableClient;
        private readonly TableClient _orderTableClient;

        public TableStorageService(string connectionString)
        {
            _customerTableClient = new TableClient(connectionString, "customer");
            _productTableClient = new TableClient(connectionString, "product");
            _orderTableClient = new TableClient(connectionString, "orders");

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

        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; // product not found
            }
        }

        public async Task UpdateProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set for the entity.");
            }

            try
            {
                await _productTableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Failed to update the product in table storage.", ex);
            }
        }
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = new List<Order>();
            await foreach (var order in _orderTableClient.QueryAsync<Order>())
            {
                orders.Add(order);
            }
            return orders;
        }
        public async Task AddOrderAsync(Order order)
        {
            try
            {
                await _orderTableClient.UpsertEntityAsync(order); 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error adding order to table storage.", ex);
            }
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.PartitionKey) || string.IsNullOrEmpty(order.RowKey))
                throw new ArgumentException("PartitionKey and RowKey must be set for the entity.");

            try
            {
                await _orderTableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Failed to update the order in table storage.", ex);
            }
        }
        public async Task DeleteOrderAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error deleting order from table storage.", ex);
            }
        }
        public async Task<Order?> GetOrderByOrderIdAsync(string orderId)
        {
            var query = _orderTableClient.QueryAsync<Order>(o => o.Order_ID == orderId);
            await foreach (var order in query)
            {
                return order; // Return the first match
            }
            return null;
        }
    }
}
