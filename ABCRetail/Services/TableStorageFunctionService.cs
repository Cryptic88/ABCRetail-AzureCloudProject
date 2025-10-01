using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    public class TableStorageFunctionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseFunctionUrl;

        public TableStorageFunctionService(HttpClient httpClient, string baseFunctionUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseFunctionUrl = baseFunctionUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseFunctionUrl));
        }

        public async Task<bool> StoreCustomerAsync(Customer customer)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseFunctionUrl}/customer", customer);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var customers = await _httpClient.GetFromJsonAsync<List<Customer>>($"{_baseFunctionUrl}/customer");
            return customers ?? new List<Customer>();
        }

        public async Task<Customer?> GetCustomerAsync(string partitionKey, string rowKey)
        {
            return await _httpClient.GetFromJsonAsync<Customer>($"{_baseFunctionUrl}/customer/{partitionKey}/{rowKey}");
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseFunctionUrl}/customer/{customer.PartitionKey}/{customer.RowKey}", customer);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"{_baseFunctionUrl}/customer/{partitionKey}/{rowKey}");
            return response.IsSuccessStatusCode;
        }


        public async Task<bool> StoreProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey))
                product.PartitionKey = "ProductsPartition";

            if (string.IsNullOrEmpty(product.RowKey))
                product.RowKey = Guid.NewGuid().ToString();

            var response = await _httpClient.PostAsJsonAsync($"{_baseFunctionUrl}/product", product);
            return response.IsSuccessStatusCode;
        }
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = await _httpClient.GetFromJsonAsync<List<Product>>($"{_baseFunctionUrl}/product")
                           ?? new List<Product>();

            foreach (var p in products)
            {
                Console.WriteLine($"Product {p.Product_Name}, ImageURL: {p.ImageURL}");
            }

            return products;
        }

        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            return await _httpClient.GetFromJsonAsync<Product>($"{_baseFunctionUrl}/product/{partitionKey}/{rowKey}");
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseFunctionUrl}/product/{product.PartitionKey}/{product.RowKey}", product);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteProductAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"{_baseFunctionUrl}/product/{partitionKey}/{rowKey}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddOrderAsync(Order order)
        {
            if (string.IsNullOrEmpty(order.PartitionKey))
                order.PartitionKey = "OrdersPartition";

            if (string.IsNullOrEmpty(order.RowKey))
                order.RowKey = Guid.NewGuid().ToString();

            var response = await _httpClient.PostAsJsonAsync($"{_baseFunctionUrl}/order", order);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            var orders = await _httpClient.GetFromJsonAsync<List<Order>>($"{_baseFunctionUrl}/order");
            return orders ?? new List<Order>();
        }

        public async Task<Order?> GetOrderAsync(string partitionKey, string rowKey)
        {
            return await _httpClient.GetFromJsonAsync<Order>($"{_baseFunctionUrl}/order/{partitionKey}/{rowKey}");
        }

        public async Task<Order?> GetOrderByOrderIdAsync(string orderId)
        {
            return await _httpClient.GetFromJsonAsync<Order>($"{_baseFunctionUrl}/order/byid/{orderId}");
        }

        public async Task<bool> UpdateOrderAsync(Order order)
        {
            var response = await _httpClient.PutAsJsonAsync($"{_baseFunctionUrl}/order/{order.PartitionKey}/{order.RowKey}", order);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteOrderAsync(string partitionKey, string rowKey)
        {
            var response = await _httpClient.DeleteAsync($"{_baseFunctionUrl}/order/{partitionKey}/{rowKey}");
            return response.IsSuccessStatusCode;
        }

    }
}

