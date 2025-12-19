using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;

namespace RolixSAEProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly CustomerAuthService _auth;

        public AuthController(CustomerAuthService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string identifiant, string motDePasse, string? returnUrl = null)
        {
            var accountId = _auth.ValidateLogin(identifiant, motDePasse);

            if (accountId == null)
            {
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.Error = "Identifiant ou mot de passe invalide.";
                return View();
            }

            HttpContext.Session.SetString("account_id", accountId);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("account_id");
            return RedirectToAction("Index", "Home");
        }
    }
}
