using ABCRetail.Data;
using ABCRetail.Filters;
using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageFunctionService _tableStorageService;
        private readonly ABCRetailDbContext _db;

        public CustomerController(TableStorageFunctionService tableStorageService, ABCRetailDbContext db)
        {
            _tableStorageService = tableStorageService;
            _db = db;
        }

        [AdminAuthorize]
        public async Task<IActionResult> IndexAsync(string searchString)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            var customer = await _tableStorageService.GetAllCustomersAsync();
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
        [AdminAuthorize]
        public async Task<IActionResult> AddCustomer(Customer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.Customer_Name))
            {
                TempData["Error"] = "Please enter a full name.";
                return View(customer);
            }
            customer.PartitionKey = "CustomersPartition";
            customer.RowKey = Guid.NewGuid().ToString();
            customer.Customer_Id = Guid.NewGuid().ToString("N").Substring(0, 8);

            var success = await _tableStorageService.StoreCustomerAsync(customer);

            if (success)
            {
                TempData["Success"] = "Customer added successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to add customer.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        [AdminAuthorize]
        public IActionResult AddCustomer()
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Details(string partitionKey, string rowKey)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            if (partitionKey == null || rowKey == null) return NotFound();

            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }

        
        [HttpGet]
        [AdminAuthorize]
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            // Check if customer is logged in
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            if (partitionKey == null || rowKey == null) return NotFound();

            var customer = await _tableStorageService.GetCustomerAsync(partitionKey, rowKey);
            if (customer == null) return NotFound();

            return View(customer);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminAuthorize]
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
        [AdminAuthorize]
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

        // GET: Customer Dashboard
        public async Task<IActionResult> CustomerIndex()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
                return RedirectToAction("Login", "Account");

            // Fetch logged-in customer only
            var customer = await _db.Customer.FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);
            if (customer == null) return NotFound();

            return View(customer); // Pass a single customer to the view
        }

        // GET: Edit customer details
        [HttpGet]
        public async Task<IActionResult> CustomerEdit()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var customer = await _db.Customer.FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Update customer details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerEdit(Customer updatedCustomer)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null) return RedirectToAction("Login", "Account");

            var customer = await _db.Customer.FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);
            if (customer == null) return NotFound();

            if (!ModelState.IsValid) return View(updatedCustomer);

            // Update fields
            customer.Name = updatedCustomer.Customer_Name;
            customer.Email = updatedCustomer.email;
            if (!string.IsNullOrWhiteSpace(updatedCustomer.password))
            {
                customer.PasswordHash = updatedCustomer.password; // Update password only if provided
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Your details were updated successfully!";
            return RedirectToAction(nameof(CustomerIndex));
        }
    }
}
/*Refrence List:
 stackoverflow, 2017. How to use Guid without alphabet (selenium c#). [online] Available at:
 https://stackoverflow.com/questions/47473815/how-to-use-guid-without-alphabet-selenium-c
 [Accessed 15 August 2025].

*/
