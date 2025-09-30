using System.Reflection;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using NuGet.Packaging.Signing;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly QueueService _queueService;
        private readonly TableStorageFunctionService _tableStorageService;

        public OrderController(QueueService queueService, TableStorageFunctionService tableStorageService)
        {
            _queueService = queueService;
            _tableStorageService = tableStorageService;
        }
        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetAllOrdersAsync();
            var customers = await _tableStorageService.GetAllCustomersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            ViewBag.Customers = customers.ToDictionary(c => c.Customer_Id.ToString(), c => c.Customer_Name);
            ViewBag.Products = products.ToDictionary(p => p.Product_ID, p => p.Product_Name);

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> PlaceOrder()
        {
            var customers = await _tableStorageService.GetAllCustomersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string customerId, string productId, int quantity)
        {
            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(productId) || quantity <= 0)
            {
                TempData["Error"] = "Please select a customer, a product, and enter a valid quantity.";

                var customers = await _tableStorageService.GetAllCustomersAsync();
                var products = await _tableStorageService.GetAllProductsAsync();
                ViewBag.Customers = customers;
                ViewBag.Products = products;

                return View();
            }

            var order = new Order
            {
                Order_ID = Guid.NewGuid().ToString(),
                Customer_Id = customerId,
                Product_ID = productId,
                Quantity = quantity,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                PartitionKey = "OrdersPartition",
                RowKey = Guid.NewGuid().ToString()
            };


            await _tableStorageService.AddOrderAsync(order);

            // Sending custom messages to the Azure Queue
            await _queueService.SendMessageAsync($"Order Placed: Order ID={order.Order_ID}, Customer ID={order.Customer_Id}, Product ID={order.Product_ID}, Quantity={order.Quantity}");
            await _queueService.SendMessageAsync($"Processing Order: Order ID={order.Order_ID}");

            // If the product has an image, send an image upload message
            var product = await _tableStorageService.GetProductAsync("ProductsPartition", productId);
            if (product != null && !string.IsNullOrEmpty(product.ImageURL))
            {
                await _queueService.SendMessageAsync($"Image Uploaded: {product.ImageURL} for Order ID={order.Order_ID}");
            }

            await _tableStorageService.AddOrderAsync(order);

            await _queueService.SendOrderMessageAsync(order);

            TempData["Message"] = "Order placed successfully!";

            return RedirectToAction("Index");
        }

        //Simulates order processing by getting the next message from the queue and then updates the order status in Table Storage
        //This simulates the delivery process
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            var existingOrder = await _tableStorageService.GetOrderByOrderIdAsync(orderId);
            if (existingOrder != null)
            {
                switch (existingOrder.Status)
                {
                    case "Pending":
                        existingOrder.Status = "Packaged";
                        break;
                    case "Packaged":
                        existingOrder.Status = "Out for Delivery";
                        break;
                    case "Out for Delivery":
                        existingOrder.Status = "Completed";
                        break;
                    default:
                        TempData["Error"] = "Order is already completed.";
                        return RedirectToAction("Index");
                }

                await _tableStorageService.UpdateOrderAsync(existingOrder);
                TempData["Message"] = $"Order {orderId} updated to {existingOrder.Status}.";
            }
            else
            {
                TempData["Error"] = "Order not found.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                TempData["Error"] = "Invalid order reference.";
                return RedirectToAction("Index");
            }

            try
            {
                await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);
                TempData["Message"] = "Order cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to cancel order: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
