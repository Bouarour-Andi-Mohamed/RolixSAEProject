using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;

namespace RolixSAEProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataverseService _dataverseService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(DataverseService dataverseService, ILogger<AccountController> logger)
        {
            _dataverseService = dataverseService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var vm = new LoginViewModel { ReturnUrl = returnUrl };

            if (IsAuthenticated())
            {
                return RedirectToReturnOrAccount(returnUrl);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var compte = _dataverseService.AuthentifierCompte(model.Identifiant, model.MotDePasse);
            if (compte == null)
            {
                ModelState.AddModelError(string.Empty, "Identifiants invalides. Veuillez réessayer.");
                return View(model);
            }

            HttpContext.Session.SetString("AccountId", compte.Id.ToString());
            HttpContext.Session.SetString("AccountName", compte.Identifiant);

            _logger.LogInformation("Connexion réussie pour {Identifiant}", compte.Identifiant);

            return RedirectToReturnOrAccount(model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout(string? returnUrl = null)
        {
            HttpContext.Session.Remove("AccountId");
            HttpContext.Session.Remove("AccountName");

            return RedirectToReturnOrAccount(returnUrl);
        }

        public IActionResult Index()
        {
            if (!IsAuthenticated())
            {
                return RedirectToAction(nameof(Login), new { returnUrl = Url.Action(nameof(Index)) });
            }

            ViewBag.AccountName = HttpContext.Session.GetString("AccountName") ?? "Mon compte";
            return View();
        }

        private bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("AccountId"));
        }

        private IActionResult RedirectToReturnOrAccount(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
