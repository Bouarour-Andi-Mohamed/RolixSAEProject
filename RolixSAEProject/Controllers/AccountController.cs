using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Services;

namespace RolixSAEProject.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly CustomerAccountService _accountService;

        public AccountController(CustomerAccountService accountService)
        {
            _accountService = accountService;
        }

        // ✅ URL: /Account
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            var accountId = HttpContext.Session.GetString("account_id");

            if (string.IsNullOrWhiteSpace(accountId))
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });
            }

            var profile = _accountService.GetProfile(accountId);

            if (profile == null)
            {
                HttpContext.Session.Remove("account_id");
                HttpContext.Session.Remove("account_name");
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });
            }

            return View("~/Views/Auth/Account.cshtml", profile);
        }
    }
}
