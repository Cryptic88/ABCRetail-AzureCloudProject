using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;

namespace ProcessOrderFunction
{
    public class OrderQueueNotifier
    {
        private readonly ILogger<OrderQueueNotifier> _logger;
        private readonly string _notificationsConnection;

        public OrderQueueNotifier(ILogger<OrderQueueNotifier> logger)
        {
            _logger = logger;
            _notificationsConnection = "DefaultEndpointsProtocol=https;AccountName=st10439009;AccountKey=REMOVED;EndpointSuffix=core.windows.net";
        }

        [Function(nameof(OrderQueueNotifier))]
        public async Task Run(
            [QueueTrigger("orders", Connection = "AzureWebJobsStorage")] string messageText)
        {
            _logger.LogInformation("===== Queue Trigger Started =====");
            _logger.LogInformation($"Raw queue message: {messageText}");

            try
            {

                var order = JsonConvert.DeserializeObject<Order>(messageText, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                if (order == null)
                {
                    _logger.LogError("Failed to deserialize order message: null result.");
                    return;
                }

                _logger.LogInformation($"Processing Order: {order.Order_ID}");
                _logger.LogInformation($"Customer ID: {order.Customer_Id}");
                _logger.LogInformation($"Product ID: {order.Product_ID}");
                _logger.LogInformation($"Quantity: {order.Quantity}");
                _logger.LogInformation($"Status: {order.Status}");
                _logger.LogInformation($"Order Date: {order.OrderDate}");

                //(Microsoft Learn, 2024)
                // Create a custom, user-friendly message
                string userMessage = $"New Order: {order.Order_ID}\n" +
                                     $"Customer: {order.Customer_Id}\n" +
                                     $"Product: {order.Product_ID}\n" +
                                     $"Quantity: {order.Quantity}\n" +
                                     $"Status: {order.Status}";

                // Send notification to the notifications queue
                var notificationsQueue = new QueueClient(_notificationsConnection, "notification");
                await notificationsQueue.CreateIfNotExistsAsync();
                await notificationsQueue.SendMessageAsync(userMessage);

                _logger.LogInformation($"Notification sent: {userMessage}");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize order message: invalid JSON.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing order message.");
                throw;
            }
        }
    }
}

/*Reference List:
  Microsoft Learn, 2024. Azure Queue storage trigger for Azure Functions. [online] Available at:
  <https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=python-v2%2Cisolated-process%2Cnodejs-v4%2Cextensionv5&pivots=programming-language-csharp> [Acccessed 5 October 2025]
*/
