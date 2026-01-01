using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;

namespace RolixSAEProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly CustomerAccountService _accountService;

        public AuthController(CustomerAccountService accountService)
        {
            _accountService = accountService;
        }

        // GET: /Auth/Login?returnUrl=/Account
        [HttpGet]
        public IActionResult Login(string? returnUrl = "/Account")
        {
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Account" : returnUrl;
            return View("~/Views/Auth/Login.cshtml", new LoginViewModel());
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = "/Account")
        {
            returnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/Account" : returnUrl;

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View("~/Views/Auth/Login.cshtml", model);
            }

            var profile = _accountService.ValidateLogin(model.Identifiant, model.MotDePasse);

            if (profile == null)
            {
                ViewBag.Error = "Login incorrect.";
                ViewBag.ReturnUrl = returnUrl;
                return View("~/Views/Auth/Login.cshtml", model);
            }

            // session OK
            HttpContext.Session.SetString("account_id", profile.AccountId);

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return Redirect("/Account");
        }


        // /Auth/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("account_id");
            HttpContext.Session.Remove("account_name");
            return RedirectToAction("Index", "Home");
        }
    }
}
