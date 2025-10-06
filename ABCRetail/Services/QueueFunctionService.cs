using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ABCRetail.Models;
using Newtonsoft.Json;

namespace ABCRetail.Services
{
    public class QueueFunctionService
    {
        private readonly HttpClient _httpClient;

        public QueueFunctionService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // Sends a human-readable order message
        public async Task SendOrderMessageAsync(Order order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            var payload = new
            {
                order.Order_ID,
                order.Customer_Id,
                order.Product_ID,
                order.Quantity,
                order.Status,
                order.OrderDate,
                order.PartitionKey,
                order.RowKey
            };

            string json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, new UTF8Encoding(false), "application/json");
            var response = await _httpClient.PostAsync("https://processorder-dda4aqf4erf6b8fh.southafricanorth-01.azurewebsites.net/api/WriteMessageQueue", content);
            response.EnsureSuccessStatusCode();
        }

        // Sends a simple custom message (like processing updates)
        public async Task SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

            var payload = new { MessageText = message };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://processorder-dda4aqf4erf6b8fh.southafricanorth-01.azurewebsites.net/api/WriteMessageQueue", content);
            response.EnsureSuccessStatusCode();
        }
    }
}


