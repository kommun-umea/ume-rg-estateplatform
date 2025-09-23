using Microsoft.AspNetCore.Mvc;

namespace Umea.se.EstateService.API.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
