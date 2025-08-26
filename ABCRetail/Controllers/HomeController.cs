using System.Diagnostics;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TableStorageService _tableStorageService;


        public HomeController(ILogger<HomeController> logger, TableStorageService tableStorageService)
        {
            _logger = logger;
            _tableStorageService = tableStorageService;
        }

        public async Task<IActionResult> IndexAsync() //Helps display featured products on the home page
        {
            var allProducts = await _tableStorageService.GetAllProductsAsync();

            var featuredProducts = allProducts.Take(3).ToList();

            return View(featuredProducts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
