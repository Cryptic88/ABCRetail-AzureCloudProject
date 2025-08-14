using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
