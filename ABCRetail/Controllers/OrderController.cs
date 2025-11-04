using System.Reflection;
using ABCRetail.Filters;
using ABCRetail.Models;
using ABCRetail.Data;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;

namespace ABCRetail.Controllers
{
    public class OrderController : Controller
    {
        private readonly QueueFunctionService _queueService;
        private readonly TableStorageFunctionService _tableStorageService;
        private readonly ABCRetailDbContext _dbContext;

        public OrderController(QueueFunctionService queueService, TableStorageFunctionService tableStorageService, ABCRetailDbContext dbContext)
        {
            _queueService = queueService;
            _tableStorageService = tableStorageService;
            _dbContext = dbContext;
        }

        [AdminAuthorize]
        public async Task<IActionResult> Index()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            // Get orders from Table Storage
            var orders = await _tableStorageService.GetAllOrdersAsync();

            // Get customers from SQL database
            var customers = await _dbContext.Customer
                .ToDictionaryAsync(c => c.CustomerId.ToString(), c => c.Name);

            // Get products from Table Storage
            var products = await _tableStorageService.GetAllProductsAsync();
            var productsDict = products.ToDictionary(p => p.Product_ID, p => p.Product_Name);

            ViewBag.Customers = customers;
            ViewBag.Products = productsDict;

            return View(orders);
        }


        public async Task<IActionResult> CustomerIndex()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var allOrders = await _tableStorageService.GetAllOrdersAsync();

            // Filter only logged-in user’s orders
            var userOrders = allOrders.Where(o => o.Customer_Id == customerId.ToString()).ToList();

            var products = await _tableStorageService.GetAllProductsAsync();
            ViewBag.Products = products.ToDictionary(p => p.Product_ID, p => p.Product_Name);

            return View(userOrders);
        }


        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> PlaceOrder()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var customers = await _tableStorageService.GetAllCustomersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        [HttpPost]
        [AdminAuthorize]
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
        [AdminAuthorize]
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
        [AdminAuthorize]
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
        [AdminAuthorize]
        public async Task<IActionResult> GetNotifications()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var orders = await _tableStorageService.GetAllOrdersAsync(); // last 10 orders
            var notifications = orders.Select(o => $"New Order: {o.RowKey}, Customer: {o.Customer_Id}, Product: {o.Product_ID}, Qty: {o.Quantity}");
            return Json(notifications);
        }

        // confirmation page
        [HttpGet]
        public IActionResult PlaceOrderFromCart()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var cartJson = HttpContext.Session.GetString("Cart");
            var cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson);

            return View(cart); // Show order confirmation page
        }

        // performs order creation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrderFromCartConfirmed()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            // Get the customer from the SQL database using your DbContext
            var customer = await _dbContext.Customer
                .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);

            ViewBag.CustomerName = customer?.Name ?? "Unknown";

            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Cart", "Index");
            }

            var cartJson = HttpContext.Session.GetString("_Cart"); // your session key
            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // Deserialize the cart JSON into a list
            var cart = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson);

            // Now create the orders in Table Storage
            foreach (var item in cart)
            {
                var order = new Order
                {
                    Order_ID = Guid.NewGuid().ToString(),
                    Customer_Id = customer.CustomerId.ToString(), // store as string in Table Storage
                    Product_ID = item.Product.Product_ID,
                    Quantity = item.Quantity,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow,
                    PartitionKey = "ORDERS",
                    RowKey = Guid.NewGuid().ToString()
                };

                await _tableStorageService.AddOrderAsync(order);
                await _queueService.SendOrderMessageAsync(order);
            }


            // Clear cart after order
            HttpContext.Session.Remove("_Cart");

            TempData["Message"] = "Your order(s) have been placed!";
            return RedirectToAction("CustomerIndex");
        }


    }
}
