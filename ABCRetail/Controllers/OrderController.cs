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

        [AdminAuthorize] //(Microsoft Learn, 2025)
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
        [AdminAuthorize] //(Microsoft Learn, 2025)
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
        [AdminAuthorize] //(Microsoft Learn, 2025)
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

            // Get the SQL Product record
            var productSql = await _dbContext.Product
                .FirstOrDefaultAsync(p => p.Name == product.Product_Name);

            if (productSql == null)
                throw new Exception("Product not found in SQL database.");

            // Create SQL Order
            var orderSql = new OrderSql
            {
                CustomerId = int.Parse(customerId),
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                TotalAmount = productSql.Price * quantity,
                OrderItems = new List<OrderItemSql>
                {
                    new OrderItemSql
                    {
                        ProductId = productSql.ProductId,
                        Quantity = quantity,
                        UnitPrice = productSql.Price
                    }
                }
            };

            _dbContext.Order.Add(orderSql);
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Order placed successfully!";
            return RedirectToAction("Index");
        }

        //Simulates order processing by getting the next message from the queue and then updates the order status in Table Storage
        //This simulates the delivery process
        [HttpPost]
        [AdminAuthorize] //(Microsoft Learn, 2025)
        public async Task<IActionResult> ProcessOrder(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
            {
                TempData["Error"] = "Invalid order reference.";
                return RedirectToAction("Index");
            }

            // Process order in Table Storage
            bool success = await _tableStorageService.ProcessOrderAsync(rowKey);

            if (!success)
            {
                TempData["Error"] = $"Failed to process order {rowKey}.";
                return RedirectToAction("Index");
            }

            // Get full order details from Table Storage
            var tableOrder = await _tableStorageService.GetOrderAsync("ORDERS", rowKey);
            if (tableOrder != null)
            {
                await _queueService.SendOrderMessageAsync(tableOrder);

                // Update corresponding SQL order status
                var sqlOrder = await _dbContext.Order
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o =>
                        o.CustomerId.ToString() == tableOrder.Customer_Id &&
                        o.OrderDate == tableOrder.OrderDate);

                if (sqlOrder != null)
                {
                    sqlOrder.Status = "Processed"; // Match Table Storage status
                    await _dbContext.SaveChangesAsync();
                }
            }

            TempData["Message"] = $"Order {rowKey} processed successfully!";
            return RedirectToAction("Index");
        }


        [HttpPost]
        [AdminAuthorize] //(Microsoft Learn, 2025)
        public async Task<IActionResult> CancelOrder(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                TempData["Error"] = "Invalid order reference.";
                return RedirectToAction("Index");
            }

            try
            {
                // Delete from Table Storage
                var order = await _tableStorageService.GetOrderAsync(partitionKey, rowKey);
                if (order != null)
                {
                    await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);

                    // Delete from SQL DB
                    var sqlOrder = await _dbContext.Order
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.CustomerId.ToString() == order.Customer_Id
                                               && o.OrderDate == order.OrderDate);

                    if (sqlOrder != null)
                    {
                        _dbContext.OrderItem.RemoveRange(sqlOrder.OrderItems); // delete items first
                        _dbContext.Order.Remove(sqlOrder); // then delete the order
                        await _dbContext.SaveChangesAsync();
                    }

                    TempData["Message"] = "Order cancelled successfully.";
                }
                else
                {
                    TempData["Error"] = "Order not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to cancel order: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        [HttpGet]
        [AdminAuthorize] //(Microsoft Learn, 2025)
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

            var cartJson = HttpContext.Session.GetString("_Cart");
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
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var customer = await _dbContext.Customer
                .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);

            ViewBag.CustomerName = customer?.Name ?? "Unknown";

            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction("Cart", "Index");
            }

            var cartJson = HttpContext.Session.GetString("_Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var cart = System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartJson);

            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // Clear cart before processing to prevent duplicate submissions
            //(Kalla, 2024)
            HttpContext.Session.Remove("_Cart");

            foreach (var item in cart)
            {
                // Create Table Storage order
                var tableOrder = new Order
                {
                    Order_ID = Guid.NewGuid().ToString(),
                    Customer_Id = customer.CustomerId.ToString(),
                    Product_ID = item.Product.Product_ID,
                    Quantity = item.Quantity,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow,
                    PartitionKey = "ORDERS",
                    RowKey = Guid.NewGuid().ToString()
                };

                await _tableStorageService.AddOrderAsync(tableOrder);
                await _queueService.SendOrderMessageAsync(tableOrder);

                // Also create SQL order
                var sqlOrder = new OrderSql
                {
                    CustomerId = customer.CustomerId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Pending",
                    TotalAmount = item.Product.Price.HasValue
                                    ? item.Product.Price.Value * item.Quantity
                                    : 0,
                    OrderItems = new List<OrderItemSql>
                    {
                        new OrderItemSql
                        {
                            ProductId = item.Product.Product_ID,
                            Quantity = item.Quantity,
                            UnitPrice = item.Product.Price.HasValue ? item.Product.Price.Value : 0
                        }
                    }
                };

                _dbContext.Order.Add(sqlOrder);
            }

            // Save all SQL orders at once
            await _dbContext.SaveChangesAsync();

            TempData["Message"] = "Your order(s) have been placed!";
            return RedirectToAction("CustomerIndex");
        }



    }
}

/*Refrence List:
  
  Microsoft Learn, 2025. Filters in ASP.NET Core. [online] Available at:
  < https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-9.0 > [Accessed 4 November 2025].

  Kalla, M., 2024. ASP.NET Core Session Management. [online] Available at:
  < https://www.c-sharpcorner.com/article/session-in-asp-net-core-mvc-net-8/ > [Accessed 4 November 2025].*/