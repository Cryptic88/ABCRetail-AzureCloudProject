using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Filters
{
    public class AdminAuthorizeAttribute : TypeFilterAttribute
    {
        // (Microsoft Learn, 2025)
        public AdminAuthorizeAttribute() : base(typeof(AdminAuthorizeFilter))
        {
        }
    }
}

/*Refrence List:
  
  Microsoft Learn, 2025. Filters in ASP.NET Core. [online] Available at:
  < https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-9.0 > [Accessed 4 November 2025].*/