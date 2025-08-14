using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
