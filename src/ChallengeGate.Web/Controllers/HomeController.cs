using Microsoft.AspNetCore.Mvc;

namespace ChallengeGate.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
