using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ABCRetail.Models;

namespace ABCRetail.Services
{
    public class QueueFunctionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseFunctionUrl;

        public QueueFunctionService(HttpClient httpClient, string baseFunctionUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseFunctionUrl = baseFunctionUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseFunctionUrl));
        }

        // Sends a human-readable order message
        public async Task SendOrderMessageAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

                var payload = new
                {
                    Order_ID = order.Order_ID,
                    Customer_Id = order.Customer_Id,
                    Product_ID = order.Product_ID,
                    Quantity = order.Quantity,
                    Status = order.Status,
                    OrderDate = order.OrderDate,
                    PartitionKey = order.PartitionKey,
                    RowKey = order.RowKey
                };
        

            string json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, new UTF8Encoding(false), "application/json");
            var response = await _httpClient.PostAsync($"{_baseFunctionUrl}/WriteMessageQueue", content);
            response.EnsureSuccessStatusCode();
        }

        // Sends a simple custom message (like processing updates)
        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

            var payload = new { MessageText = message };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseFunctionUrl}/WriteMessageQueue", content);
            response.EnsureSuccessStatusCode();
        }
    }
}

