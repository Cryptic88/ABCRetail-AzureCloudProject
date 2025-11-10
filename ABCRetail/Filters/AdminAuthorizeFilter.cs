using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ABCRetail.Data;
using System.Linq;

namespace ABCRetail.Filters
{
    public class AdminAuthorizeFilter : IAuthorizationFilter
    {
        private readonly ABCRetailDbContext _db;

        public AdminAuthorizeFilter(ABCRetailDbContext db)
        {
            _db = db;
        }

        public void OnAuthorization(AuthorizationFilterContext context) // (Microsoft Learn, 2025)
        {
            var customerId = context.HttpContext.Session.GetInt32("CustomerId");
            if (customerId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var user = _db.Customer.FirstOrDefault(c => c.CustomerId == customerId);
            if (user == null || !user.IsAdmin)
            {
                // Non-admin users redirected to home page
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}

/*Refrence List:
  
  Microsoft Learn, 2025. Filters in ASP.NET Core. [online] Available at:
  < https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-9.0 > [Accessed 4 November 2025].*/
