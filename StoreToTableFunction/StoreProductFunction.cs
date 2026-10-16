using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace StoreToTableFunction
{
    public class StoreProductFunction
    {
        private readonly ILogger<StoreProductFunction> _logger;
        private readonly TableClient _productTableClient;

        public StoreProductFunction(ILogger<StoreProductFunction> logger)
        {
            _logger = logger;
            var connectionString = Environment.GetEnvironmentVariable("StorageConnection")
                ?? throw new InvalidOperationException("StorageConnection is not configured.");
            _productTableClient = new TableClient(connectionString, "product");
            _productTableClient.CreateIfNotExists();
        }

        [Function("StoreProduct")]
        public async Task<HttpResponseData> StoreProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "product")] HttpRequestData req)
        {
            _logger.LogInformation("StoreProduct function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var product = JsonSerializer.Deserialize<Product>(requestBody, options); //(CSharp TV, 2024)

            if (product == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid product data.");
                return badResponse;
            }


            if (string.IsNullOrEmpty(product.PartitionKey))
                product.PartitionKey = "ProductsPartition";
            if (string.IsNullOrEmpty(product.RowKey))
                product.RowKey = Guid.NewGuid().ToString();

            try
            {
                await _productTableClient.AddEntityAsync(product);
                var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await response.WriteStringAsync($"Product {product.RowKey} stored successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing product in Table Storage.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error storing product.");
                return response;
            }
        }

        [Function("GetAllProducts")]
        public async Task<HttpResponseData> GetAllProducts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "product")] HttpRequestData req)
        {
            _logger.LogInformation("GetAllProducts function triggered.");

            try
            {
                var products = new List<Product>();
                await foreach (var product in _productTableClient.QueryAsync<Product>())
                {
                    products.Add(product);
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(products);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error retrieving products.");
                return response;
            }
        }

        [Function("GetProductByKey")]
        public async Task<HttpResponseData> GetProductByKey(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "product/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"GetProductByKey function triggered for {partitionKey}/{rowKey}.");

            try
            {
                var result = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result.Value);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Product not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error retrieving product.");
                return response;
            }
        }

        [Function("UpdateProduct")]
        public async Task<HttpResponseData> UpdateProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "product/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"UpdateProduct function triggered for {partitionKey}/{rowKey}.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var product = JsonSerializer.Deserialize<Product>(requestBody); //(CSharp TV, 2024)

            if (product == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid product data.");
                return badResponse;
            }

            product.PartitionKey = partitionKey;
            product.RowKey = rowKey;

            try
            {
                await _productTableClient.UpdateEntityAsync(product, ETag.All, TableUpdateMode.Replace);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Product {rowKey} updated successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error updating product.");
                return response;
            }
        }

        [Function("DeleteProduct")]
        public async Task<HttpResponseData> DeleteProduct(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "product/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"DeleteProduct function triggered for {partitionKey}/{rowKey}.");

            try
            {
                await _productTableClient.DeleteEntityAsync(partitionKey, rowKey);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Product {rowKey} deleted successfully.");
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Product not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error deleting product.");
                return response;
            }
        }
    }
}

/*Reference List:
 
CSharp TV, 2024. Leveraging Azure Tables with Azure Functions - Azure Essentials Series - Ep: 1. [video online] Available at:
<https://www.youtube.com/watch?v=3L7m0gXbtPY&t=2065s> [Accessed 30 September 2025].

 */