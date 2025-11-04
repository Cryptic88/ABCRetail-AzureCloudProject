using ABCRetail.Models;
using ABCRetail.Services;
using ABCRetail.Filters;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class FileController : Controller
    {
        private readonly AzureFileShareService _fileShareService;
        private const string DirectoryName = "dummycontracts";

        public FileController(AzureFileShareService fileShareService)
        {
            _fileShareService = fileShareService;
        }

        [AdminAuthorize]
        public async Task<IActionResult> Index()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            List<FileModel> files;
            try
            {
                files = await _fileShareService.ListFilesAsync(DirectoryName);
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Failed to load files: {ex.Message}";
                files = new List<FileModel>();
            }
            return View(files);
        }

        [HttpPost]
        [AdminAuthorize]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "Please select a file to upload");
                var files = await _fileShareService.ListFilesAsync("dummycontracts");
                return View("Index", files); 
            }

            try
            {
                using var stream = file.OpenReadStream();
                await _fileShareService.UploadFileAsync("dummycontracts", file.FileName, stream);
                TempData["Message"] = $"File '{file.FileName}' uploaded successfully";
            }
            catch (Exception e)
            {
                TempData["Message"] = $"File upload failed: {e.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(fileName))
                return BadRequest("File name cannot be null or empty");

            try
            {
                var fileStream = await _fileShareService.DownloadFileAsync(DirectoryName, fileName);
                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception e)
            {
                return BadRequest($"Error downloading file: {e.Message}");
            }
        }
    }
}
