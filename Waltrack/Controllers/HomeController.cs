using System.Diagnostics;
using Waltrack.Models;
using Microsoft.AspNetCore.Mvc;

namespace Waltrack.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
           if(User.Identity != null && User.Identity.IsAuthenticated)
            {
                // If logged in, redirect to the main dashboard
                return RedirectToAction("Index","Dashboard");
            }

            // If not logged in, show the public welcome/landing page
            return View("Welcome");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
