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
        public async Task<IActionResult> IndexAsync(string searchString)
        {
            var customer = await _tableStorageService.GetAllCustomerAsync();
            //Add on for a search function just to make it easier to find products
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                customer = customer.Where(c => !string.IsNullOrEmpty(c.Customer_Name) &&
                                c.Customer_Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.SearchString = searchString ?? "";
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            customer.PartitionKey = "CustomersPartition";
            customer.RowKey = Guid.NewGuid().ToString();
            customer.Customer_Id = Math.Abs(Guid.NewGuid().GetHashCode()); //(stackoverflow, 2017)

            await _tableStorageService.AddCustomerAsync(customer);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult AddCustomer()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        
        [HttpGet]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            if (partitionKey == null || rowKey == null) return NotFound();

            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Customer updatedCustomer)
        {
            if (!ModelState.IsValid)
                return View(updatedCustomer);

            // Gets the existing customer
            var existing = await _tableStorageService.GetCustomerAsync(updatedCustomer.PartitionKey, updatedCustomer.RowKey);
            if (existing == null) return NotFound();

            // Updates fields (keeps password if not changed)
            existing.Customer_Name = updatedCustomer.Customer_Name;
            existing.email = updatedCustomer.email;
            if (!string.IsNullOrWhiteSpace(updatedCustomer.password))
            {
                existing.password = updatedCustomer.password; // replaces password only if user entered one
            }

            await _tableStorageService.UpdateCustomerAsync(existing);

            TempData["Success"] = "Customer updated successfully!";
            return RedirectToAction(nameof(Index));
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
/*Refrence List:
 stackoverflow, 2017. How to use Guid without alphabet (selenium c#). [online] Available at:
 https://stackoverflow.com/questions/47473815/how-to-use-guid-without-alphabet-selenium-c
 [Accessed 15 August 2025].

*/
