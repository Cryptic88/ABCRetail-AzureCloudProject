using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableStorageService _tableStorageService;

        public ProductController(BlobService blobService, TableStorageService tableStorageService)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
        }
        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product products, IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                var imageUrl = await _blobService.UploadsAsync(stream, file.FileName);
                products.ImageURL = imageUrl;
            }

            if (ModelState.IsValid)
            {
                products.PartitionKey = "ProductsPartition";
                products.RowKey = Guid.NewGuid().ToString();
                products.Product_ID = $"PROD-{Guid.NewGuid().ToString().Substring(0, 8)}";
                await _tableStorageService.AddProductAsync(products);
                return RedirectToAction("Index");
            }

            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey, Product products)
        {
            if (products != null && !string.IsNullOrEmpty(products.ImageURL))
            {
                await _blobService.DeleteBlobAsync(products.ImageURL);
            }

            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
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
        public async Task<IActionResult> Edit(Product product, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                // If a new file was uploaded, replace the old one
                if (file != null)
                {
                    // delete old blob if exists
                    if (!string.IsNullOrEmpty(product.ImageURL))
                    {
                        await _blobService.DeleteBlobAsync(product.ImageURL);
                    }

                    using var stream = file.OpenReadStream();
                    var imageUrl = await _blobService.UploadsAsync(stream, file.FileName);
                    product.ImageURL = imageUrl;
                }

                await _tableStorageService.UpdateProductAsync(product);
                return RedirectToAction("Index");
            }

            return View(product);
        }
    }
}
