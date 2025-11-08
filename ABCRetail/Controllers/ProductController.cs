using ABCRetail.Data;
using ABCRetail.Filters;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly BlobFunctionService _blobService;
        private readonly TableStorageFunctionService _tableStorageService;
        private readonly ABCRetailDbContext _context;

        public ProductController(BlobFunctionService blobService, TableStorageFunctionService tableStorageService, ABCRetailDbContext context)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> CustomerIndex(string searchString)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var products = await _tableStorageService.GetAllProductsAsync();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(p => !string.IsNullOrEmpty(p.Product_Name) &&
                                               p.Product_Name.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                                   .ToList();
            }

            ViewBag.SearchString = searchString ?? "";

            return View(products);
        }

        [AdminAuthorize]
        public async Task<IActionResult> Index(string searchString)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var products = await _tableStorageService.GetAllProductsAsync();

            //Add on for a search function just to make it easier to find products
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(p => !string.IsNullOrEmpty(p.Product_Name) &&
                                p.Product_Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.SearchString = searchString ?? "";

            return View(products);
        }

        [HttpGet]
        [AdminAuthorize]
        public IActionResult AddProduct()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> AddProduct(Product product, IFormFile file)
        {
            // Manual validation
            if (string.IsNullOrWhiteSpace(product.Product_Name) || product.Price <= 0)
            {
                TempData["Error"] = "Please provide a valid product name and price.";
                return View(product);
            }

            // Ensure Product_ID
            if (string.IsNullOrEmpty(product.Product_ID))
                product.Product_ID = $"PROD-{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Handle file upload
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadsAsync(stream, file.FileName, product.Product_ID);
                Console.WriteLine($"Blob URL: {imageUrl}");
                product.ImageURL = imageUrl;
            }

            // Ensure Partition & Row keys are always set
            product.PartitionKey = "ProductsPartition";
            product.RowKey = Guid.NewGuid().ToString();

            // Store product
            await _tableStorageService.StoreProductAsync(product);

            // Also store info in SQL DB
            var productSql = new ProductSql
            {
                Name = product.Product_Name,
                Description = product.Description,
                ImageUrl = product.ImageURL,
                Price = product.Price.HasValue ? (decimal)product.Price.Value : 0,
                StockQty = 10 
            };

            _context.Product.Add(productSql);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product != null && !string.IsNullOrEmpty(product.ImageURL))
            {
                await _blobService.DeleteBlobAsync(product.ImageURL);
            }

            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            var productSql = _context.Product.FirstOrDefault(p => p.Name == product.Product_Name);

            if (productSql != null)
            {
                _context.Product.Remove(productSql);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");


            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(Product product, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // Ensure Product_ID is set
                if (string.IsNullOrEmpty(product.Product_ID))
                {
                    product.Product_ID = $"PROD-{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                if (file != null)
                {
                    // delete old blob if exists
                    if (!string.IsNullOrEmpty(product.ImageURL))
                    {
                        await _blobService.DeleteBlobAsync(product.ImageURL);
                    }

                    using var stream = file.OpenReadStream();
                    var imageUrl = await _blobService.UploadsAsync(stream, file.FileName, product.Product_ID);
                    Console.WriteLine($"Blob URL: {imageUrl}");
                    product.ImageURL = imageUrl;
                }

                await _tableStorageService.UpdateProductAsync(product);

                // update the SQL db aswell
                var existingProductSql = _context.Product.FirstOrDefault(p => p.Name == product.Product_Name);

                if (existingProductSql != null)
                {
                    existingProductSql.Name = product.Product_Name;
                    existingProductSql.Description = product.Description;
                    existingProductSql.ImageUrl = product.ImageURL;
                    existingProductSql.Price = product.Price.HasValue ? (decimal)product.Price.Value : existingProductSql.Price;

                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }

            return View(product);
        }

        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
                return NotFound();

            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
                return NotFound();

            return View(product);
        }
    }
}
/*Refrence List:
 stackoverflow, 2020. Is there any way to get the first 5 characters of a GUID?. [online] Available at:
 https://stackoverflow.com/questions/61124190/is-there-any-way-to-get-the-first-5-characters-of-a-guid
 [Accessed 15 August 2025].*/