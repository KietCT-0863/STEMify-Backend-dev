using Microsoft.AspNetCore.Mvc;

namespace Identity.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult TestOAuth()
    {
        return View();
    }

    [HttpGet("/test")]
    public IActionResult Test()
    {
        return Json(new { message = "HomeController is working!", timestamp = DateTime.Now });
    }
}
