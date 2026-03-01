using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Ok();
        }
    }
}
