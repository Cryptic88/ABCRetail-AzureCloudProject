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
    public class StoreOrderFunction
    {
        private readonly ILogger<StoreOrderFunction> _logger;
        private readonly TableClient _orderTableClient;

        public StoreOrderFunction(ILogger<StoreOrderFunction> logger)
        {
            _logger = logger;
            var connectionString = Environment.GetEnvironmentVariable("StorageConnection")
                ?? throw new InvalidOperationException("StorageConnection is not configured.");
            _orderTableClient = new TableClient(connectionString, "orders");
            _orderTableClient.CreateIfNotExists();
        }

        [Function("StoreOrder")]
        public async Task<HttpResponseData> StoreOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order")] HttpRequestData req)
        {
            _logger.LogInformation("StoreOrder function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var order = JsonSerializer.Deserialize<Order>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid order data.");
                return badResponse;
            }

            if (string.IsNullOrEmpty(order.PartitionKey))
                order.PartitionKey = "ORDERS";
            if (string.IsNullOrEmpty(order.RowKey))
                order.RowKey = Guid.NewGuid().ToString();

            try
            {
                await _orderTableClient.AddEntityAsync(order);
                var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await response.WriteStringAsync($"Order {order.RowKey} stored successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing order in Table Storage.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error storing order.");
                return response;
            }
        }

        [Function("GetAllOrders")]
        public async Task<HttpResponseData> GetAllOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "order")] HttpRequestData req)
        {
            _logger.LogInformation("GetAllOrders function triggered.");

            try
            {
                var orders = new List<Order>();
                await foreach (var order in _orderTableClient.QueryAsync<Order>())
                {
                    orders.Add(order);
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(orders);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error retrieving orders.");
                return response;
            }
        }

        [Function("GetOrder")]
        public async Task<HttpResponseData> GetOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "order/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"GetOrder function triggered for {partitionKey}/{rowKey}.");

            try
            {
                var result = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result.Value);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Order not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error retrieving order.");
                return response;
            }
        }

        [Function("UpdateOrder")]
        public async Task<HttpResponseData> UpdateOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "order/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"UpdateOrder function triggered for {partitionKey}/{rowKey}.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updatedData = JsonSerializer.Deserialize<Order>(requestBody); //(CSharp TV, 2024)

            if (updatedData == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid order data.");
                return badResponse;
            }

            try
            {
                // Fetch existing entity
                var existingEntityResponse = await _orderTableClient.GetEntityAsync<Order>(partitionKey, rowKey);
                var existingEntity = existingEntityResponse.Value;

                if (!string.IsNullOrEmpty(updatedData.Status))
                {
                    existingEntity.Status = updatedData.Status;
                }

                if (updatedData.Quantity > 0)
                {
                    existingEntity.Quantity = updatedData.Quantity;
                }

                // Update the entity in Table Storage
                await _orderTableClient.UpdateEntityAsync(existingEntity, ETag.All, TableUpdateMode.Replace);

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Order {rowKey} updated successfully.");
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Order not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error updating order.");
                return response;
            }
        }

        [Function("ProcessOrder")]
        public async Task<HttpResponseData> ProcessOrder(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "order/process/{rowKey}")] HttpRequestData req,
        string rowKey)
        {
            _logger.LogInformation($"ProcessOrder function triggered for RowKey: {rowKey}");

            try
            {
                // Fetch existing order
                var orderResponse = await _orderTableClient.GetEntityAsync<Order>("ORDERS", rowKey);
                var order = orderResponse.Value;

                // Update status progression
                order.Status = order.Status switch
                {
                    "Pending" => "Packaged",
                    "Packaged" => "Out for Delivery",
                    "Out for Delivery" => "Completed",
                    "Completed" => "Completed",
                    _ => order.Status
                }; // (Microsoft Learn, 2022)

                // Save the updated order
                await _orderTableClient.UpdateEntityAsync(order, ETag.All, TableUpdateMode.Replace);

                var queueConnection = Environment.GetEnvironmentVariable("connection")!;
                var queueClient = new Azure.Storage.Queues.QueueClient(queueConnection, "orders");
                await queueClient.CreateIfNotExistsAsync();

                string notificationMessage = $"Order {order.RowKey} for Customer {order.Customer_Id} is now '{order.Status}'.";
                await queueClient.SendMessageAsync(notificationMessage);

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Order {rowKey} updated to {order.Status} and notification sent.");
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Order not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error processing order.");
                return response;
            }
        }


        [Function("DeleteOrder")]
        public async Task<HttpResponseData> DeleteOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "order/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"DeleteOrder function triggered for {partitionKey}/{rowKey}.");

            try
            {
                await _orderTableClient.DeleteEntityAsync(partitionKey, rowKey);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync($"Order {rowKey} deleted successfully.");
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Order not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error deleting order.");
                return response;
            }
        }
    }
}

/*Reference List:
 
CSharp TV, 2024. Leveraging Azure Tables with Azure Functions - Azure Essentials Series - Ep: 1. [video online] Available at:
<https://www.youtube.com/watch?v=3L7m0gXbtPY&t=2065s> [Accessed 30 September 2025].

Microsoft Learn, 2022. switch expression - pattern matching expressions using the switch keyword. [online] Available at:
<https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression> [Accessed 30 September 2025].
*/