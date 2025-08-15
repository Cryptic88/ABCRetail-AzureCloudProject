using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        public CustomerController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }
        public async Task<IActionResult> IndexAsync()
        {
            var customer = await _tableStorageService.GetAllCustomerAsync();
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            customer.PartitionKey = "CustomersPartition";
            customer.RowKey = Guid.NewGuid().ToString();
            customer.Customer_Id = Math.Abs(Guid.NewGuid().GetHashCode());

            await _tableStorageService.AddCustomerAsync(customer);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                TempData["Error"] = "Customer could not be found.";
                return RedirectToAction("Index");
            }

            await _tableStorageService.DeleteCustomerAsync(partitionKey, rowKey);

            TempData["Success"] = "Customer deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
