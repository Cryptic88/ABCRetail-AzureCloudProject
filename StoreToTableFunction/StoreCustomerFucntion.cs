using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace StoreToTableFunction
{
    public class StoreCustomerFunction
    {
        private readonly ILogger<StoreCustomerFunction> _logger;
        private readonly TableClient _customerTableClient;

        public StoreCustomerFunction(ILogger<StoreCustomerFunction> logger)
        {
            _logger = logger;
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=st10439009;AccountKey=REMOVED;EndpointSuffix=core.windows.net";
            _customerTableClient = new TableClient(connectionString, "customer");
            _customerTableClient.CreateIfNotExists();
        }

        [Function("StoreCustomer")]
        public async Task<HttpResponseData> StoreCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer")] HttpRequestData req)
        {
            _logger.LogInformation("StoreCustomer function triggered.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var customer = JsonSerializer.Deserialize<Customer>(requestBody, options); //(CSharp TV, 2024)

            if (customer == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer data.");
                return badResponse;
            }

            if (string.IsNullOrEmpty(customer.Customer_Id))
                customer.Customer_Id = Guid.NewGuid().ToString();

            if (string.IsNullOrEmpty(customer.PartitionKey))
                customer.PartitionKey = "CustomersPartition";
            if (string.IsNullOrEmpty(customer.RowKey))
                customer.RowKey = Guid.NewGuid().ToString();

            try
            {
                await _customerTableClient.AddEntityAsync(customer);
                var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
                await response.WriteStringAsync($"Customer {customer.RowKey} stored successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing customer in Table Storage.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error storing customer.");
                return response;
            }
        }

        [Function("GetAllCustomers")]
        public async Task<HttpResponseData> GetAllCustomers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer")] HttpRequestData req)
        {
            _logger.LogInformation("GetAllCustomers function triggered.");

            try
            {
                var customers = new List<Customer>();

                await foreach (var customer in _customerTableClient.QueryAsync<Customer>())
                {
                    customers.Add(customer);
                }

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(customers);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error retrieving customers.");
                return response;
            }
        }


        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "customer/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"GetCustomer function triggered for {partitionKey}/{rowKey}.");

            try
            {
                var result = await _customerTableClient.GetEntityAsync<Customer>(partitionKey, rowKey);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(result.Value);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Customer not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error retrieving customer.");
                return response;
            }
        }

        [Function("UpdateCustomer")]
        public async Task<HttpResponseData> UpdateCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "customer/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"UpdateCustomer function triggered for {partitionKey}/{rowKey}.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var customer = JsonSerializer.Deserialize<Customer>(requestBody); //(CSharp TV, 2024)

            if (customer == null)
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid customer data.");
                return badResponse;
            }

            try
            {
                await _customerTableClient.UpdateEntityAsync(customer, ETag.All, TableUpdateMode.Replace);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync("Customer updated successfully.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error updating customer.");
                return response;
            }
        }

        [Function("DeleteCustomer")]
        public async Task<HttpResponseData> DeleteCustomer(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "customer/{partitionKey}/{rowKey}")] HttpRequestData req,
            string partitionKey,
            string rowKey)
        {
            _logger.LogInformation($"DeleteCustomer function triggered for {partitionKey}/{rowKey}.");

            try
            {
                await _customerTableClient.DeleteEntityAsync(partitionKey, rowKey);
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync("Customer deleted successfully.");
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var response = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await response.WriteStringAsync("Customer not found.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer.");
                var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Error deleting customer.");
                return response;
            }
        }
    }
}

/*Reference List:
 
CSharp TV, 2024. Leveraging Azure Tables with Azure Functions - Azure Essentials Series - Ep: 1. [video online] Available at:
<https://www.youtube.com/watch?v=3L7m0gXbtPY&t=2065s> [Accessed 30 September 2025].

 */
