using ABCRetail.Models;
using Azure.Storage.Queues;

namespace ABCRetail.Services
{
    public class QueueService
    {
        private readonly QueueClient _queueClient;

        public QueueService(string connectionString, string queueName)
        {
            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists(); //(Microsoft Learn, 2025)
        }
        public async Task SendMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }

        //Used for custom message structure
        public async Task SendOrderMessageAsync(Order order)
        {

            string message = $"{order.Order_ID}|{order.Customer_Id}|{order.Product_ID}|{order.Quantity}|{order.RowKey}";
            await _queueClient.SendMessageAsync(message);
        }

        public async Task<Order?> ReceiveOrderMessageAsync()
        {
            var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 1); //(Microsoft Learn, 2023)
            if (response.Value.Length == 0) return null;

            var message = response.Value[0];


            string text = message.MessageText;


            string prefix = "Order ID=";
            int startIndex = text.IndexOf(prefix);
            if (startIndex == -1)
            {
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt); //(Microsoft Learn, 2023)
                return null;
            }

            startIndex += prefix.Length;
            int endIndex = text.IndexOf(',', startIndex);
            string orderId = (endIndex == -1) ? text[startIndex..].Trim() : text[startIndex..endIndex].Trim(); //(stackoverflow, 2022)

            // Delete the message from the queue
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt); //(Microsoft Learn, 2023)

            return new Order
            {
                Order_ID = orderId
            };
        }

        // Receive message and parse it into Order object
        public async Task<Order?> ReceiveMessageAsync() //(Microsoft Learn, 2023)
        {
            var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 1);
            if (response.Value.Length == 0) return null;

            var message = response.Value[0];
            string content = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));

            // Delete the message from the queue
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);

            var parts = content.Split('|');
            if (parts.Length != 4) return null;

            return new Order
            {
                Order_ID = parts[0],
                Customer_Id = parts[1],
                Product_ID = parts[2],
                Quantity = int.Parse(parts[3]),
                Status = "Pending"
            };
        }
    }
}
/*Refrence List:
Microsoft Learn, 2025. QueueClient.CreateIfNotExists Method. [online] Available at:
https://learn.microsoft.com/en-us/dotnet/api/azure.storage.queues.queueclient.createifnotexists?view=azure-dotnet
[Accessed 22 August 2025].

Microsoft Learn, 2023. Quickstart: Azure Queue Storage client library for .NET. [online] Available at:
https://learn.microsoft.com/en-us/azure/storage/queues/storage-quickstart-queues-dotnet?tabs=passwordless%2Croles-azure-portal%2Cenvironment-variable-windows%2Csign-in-azure-cli
[Accessed 22 August 2025].

stackoverflow, 2022. Split string and get first value only. [online] Available at:
https://stackoverflow.com/questions/10868517/split-string-and-get-first-value-only
[Accessed 22 August 2025].
*/