using System;
using System;
using System.IO;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ProcessOrderFunction;

public class WriteMessageQueue
{
    private readonly ILogger<WriteMessageQueue> _logger;
    private readonly string _connectionString;

    public WriteMessageQueue(ILogger<WriteMessageQueue> logger)
    {
        _logger = logger;
        var conn = Environment.GetEnvironmentVariable("StorageConnection")
            ?? throw new InvalidOperationException("StorageConnection is not configured.");

        _connectionString = conn;
    }

    //(Microsoft Learn, 2024)
    [Function("WriteMessageQueue")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("Processing request to send an order message to the queue.");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var order = JsonConvert.DeserializeObject<Order>(requestBody, new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            });

            if (order == null ||
                string.IsNullOrWhiteSpace(order.Order_ID) ||
                string.IsNullOrWhiteSpace(order.Customer_Id) ||
                string.IsNullOrWhiteSpace(order.Product_ID))
            {
                _logger.LogWarning("Invalid or incomplete order data received.");
                return new BadRequestObjectResult("Order data is missing required fields.");
            }

            string message = JsonConvert.SerializeObject(order);

            var queueClient = new QueueClient(_connectionString, "orders");
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));


            _logger.LogInformation($"Order message sent to queue: {order.Order_ID}");
            return new OkObjectResult("Order message sent to queue successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order message to queue");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

/*Reference List:
  
 Microsoft Learn, 2024. Add messages to an Azure Storage queue using Functions. [online] Available at:
 <https://learn.microsoft.com/en-us/azure/azure-functions/functions-integrate-storage-queue-output-binding?tabs=csharp> [Accessed 2 October 2025]*/
