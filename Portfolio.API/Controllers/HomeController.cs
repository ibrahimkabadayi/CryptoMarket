using Microsoft.AspNetCore.Mvc;

namespace Portfolio.API.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
