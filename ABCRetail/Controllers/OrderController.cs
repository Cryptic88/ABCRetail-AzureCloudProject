using System.Reflection;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using NuGet.Packaging.Signing;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly QueueFunctionService _queueService;
        private readonly TableStorageFunctionService _tableStorageService;

        public OrderController(QueueFunctionService queueService, TableStorageFunctionService tableStorageService)
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

            var customer = await _tableStorageService.GetCustomerByCustomerIdAsync(customerId);
            var product = await _tableStorageService.GetProductByProductIdAsync(productId);

            var order = new Order
            {
                Order_ID = Guid.NewGuid().ToString(),
                Customer_Id = customerId,
                Product_ID = productId,
                Quantity = quantity,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                PartitionKey = "ORDERS",
                RowKey = Guid.NewGuid().ToString()
            };

            await _tableStorageService.AddOrderAsync(order);

            // Only send the properly formatted order to the queue
            await _queueService.SendOrderMessageAsync(order);

            TempData["Message"] = "Order placed successfully!";
            return RedirectToAction("Index");
        }

        //Simulates order processing by getting the next message from the queue and then updates the order status in Table Storage
        //This simulates the delivery process
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                TempData["Error"] = "Invalid order reference.";
                return RedirectToAction("Index");
            }

            bool success = await _tableStorageService.ProcessOrderAsync(rowKey);

            if (!success)
            {
                TempData["Error"] = $"Failed to process order {rowKey}.";
                return RedirectToAction("Index");
            }

            // Get full order details
            var order = await _tableStorageService.GetOrderAsync("ORDERS", rowKey);
            if (order != null)
            {
                await _queueService.SendOrderMessageAsync(order);
            }

            TempData["Message"] = $"Order {rowKey} processed successfully!";
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

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var orders = await _tableStorageService.GetAllOrdersAsync(); // last 10 orders
            var notifications = orders.Select(o => $"New Order: {o.RowKey}, Customer: {o.Customer_Id}, Product: {o.Product_ID}, Qty: {o.Quantity}");
            return Json(notifications);
        }

    }
}
