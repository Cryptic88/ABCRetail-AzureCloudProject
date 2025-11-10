using System.Security.Cryptography;
using System.Text;
using ABCRetail.Data;
using ABCRetail.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class AccountController : Controller
    {
        private readonly ABCRetailDbContext _db;

        public AccountController(ABCRetailDbContext db)
        {
            _db = db;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(CustomerSql model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Content("ModelState invalid: " + errors);
            }

            if (ModelState.IsValid)
            {
                if (_db.Customer.Any(c => c.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email already registered");
                    return View(model);
                }

                // Hash password
                model.PasswordHash = HashPassword(model.PasswordHash);

                _db.Customer.Add(model);
                _db.SaveChanges();

                // Redirect to Home
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Please enter both email and password.";
                return View();
            }

            var hashedPassword = HashPassword(password);

            var customer = _db.Customer
                .FirstOrDefault(c => c.Email == email && c.PasswordHash == hashedPassword);

            if (customer == null)
            {
                TempData["Error"] = "Invalid email or password.";
                return View();
            }

            // Store customer ID in session
            // (Gudmestad, 2024)
            HttpContext.Session.SetInt32("CustomerId", customer.CustomerId);
            HttpContext.Session.SetString("CustomerName", customer.Name);
            HttpContext.Session.SetString("IsAdmin", customer.IsAdmin ? "true" : "false");

            // Redirect to Home after login
            return RedirectToAction("CustomerIndex", "Product");
        }
        private string HashPassword(string password)
        {
            //(Chand, 2023)
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}

/*Reference List:
  
  Gudmestad, E., 2024. Tutorial - ASP.NET MVC Sessions. [video online] Available at:
  < https://www.youtube.com/watch?v=tsyTnFRnooo > [Accessed 3 November 2025].

  Chand, M., 2023. Compute SHA256 Hash In C#. [online] Available at:
  < https://www.c-sharpcorner.com/article/compute-sha256-hash-in-c-sharp/ > [Accessed 3 November 2025].*/