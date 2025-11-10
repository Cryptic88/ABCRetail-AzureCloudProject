using Microsoft.AspNetCore.Mvc;
using ABCRetail.Models;
using ABCRetail.Services;
using System.Text.Json;

namespace ABCRetail.Controllers
{
    public class CartController : Controller
    {
        //(Gudmesatd, 2024)
        private const string SessionKeyCart = "_Cart";
        private readonly TableStorageFunctionService _tableStorageService;

        public CartController(TableStorageFunctionService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        public IActionResult Index()
        {
            // Check if customer is logged in
            // (Gudmestad, 2024)
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var cart = GetCartFromSession();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            //(Gudmestad, 2024)
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");
            var cart = GetCartFromSession();

            // Get product from table storage
            var allProducts = await _tableStorageService.GetAllProductsAsync();
            var product = allProducts.FirstOrDefault(p => p.Product_ID == productId);
            if (product == null)
            {
                TempData["Message"] = "Product not found!";
                return RedirectToAction("CustomerIndex", "Product");
            }

            // Check if product already in cart
            var item = cart.FirstOrDefault(i => i.Product.Product_ID == productId);
            if (item != null)
                item.Quantity += quantity;
            else
                cart.Add(new CartItem { Product = product, Quantity = quantity });

            SaveCartToSession(cart);
            TempData["Message"] = "Product added to cart!";
            return RedirectToAction("CustomerIndex", "Product");
        }

        [HttpPost]
        public IActionResult Remove(string productId)
        {
            var cart = GetCartFromSession();
            cart.RemoveAll(i => i.Product.Product_ID == productId);
            SaveCartToSession(cart);
            return RedirectToAction("Index");
        }

        // Clear entire cart
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(SessionKeyCart);
            return RedirectToAction("Index");
        }

        // Helpers
        private List<CartItem> GetCartFromSession()
        {
            //(Gudmestad, 2024)
            var sessionData = HttpContext.Session.GetString(SessionKeyCart);
            return string.IsNullOrEmpty(sessionData)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(sessionData);
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            //(Gudmestad, 2024)
            var sessionData = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(SessionKeyCart, sessionData);
        }
    }
}

/*Reference List:
  
  Gudmestad, E., 2024. Tutorial - ASP.NET MVC Sessions. [video online] Available at:
  < https://www.youtube.com/watch?v=tsyTnFRnooo > [Accessed 3 November 2025].*/