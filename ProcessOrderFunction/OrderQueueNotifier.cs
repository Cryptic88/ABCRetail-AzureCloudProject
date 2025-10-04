using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetail.Models;
using Newtonsoft.Json;

namespace ProcessOrderFunction
{
    public class OrderQueueNotifier
    {
        private readonly ILogger<OrderQueueNotifier> _logger;

        public OrderQueueNotifier(ILogger<OrderQueueNotifier> logger)
        {
            _logger = logger;
        }

        [Function(nameof(OrderQueueNotifier))]
        public void Run(
            [QueueTrigger("orders", Connection = "connection")] string messageText)
        {
            _logger.LogInformation("===== Queue Trigger Started =====");

            try
            {
                // Use Newtonsoft.Json for maximum compatibility with MVC sender
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
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize order message: invalid JSON.");
                // Do not swallow the exception if you want Azure Functions to move it to poison queue
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
