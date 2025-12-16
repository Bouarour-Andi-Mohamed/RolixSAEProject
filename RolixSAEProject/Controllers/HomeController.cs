using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;


namespace RolixSAEProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataverseService _dataverseService;

        public HomeController(ILogger<HomeController> logger, DataverseService dataverseService)
        {
            _logger = logger;
            _dataverseService = dataverseService;
        }

        public IActionResult Index()
        {
            var produits = _dataverseService
                .GetProduitsRolix()
                .Take(3)
                .ToList();

            return View(produits);
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

        private string ResolveCurrency()
        {
            var selected = Request.Cookies["Currency"];

            return selected switch
            {
                "CHF" => "CHF",
                "USD" => "USD",
                _ => "EUR"
            };
        }
    }


}
