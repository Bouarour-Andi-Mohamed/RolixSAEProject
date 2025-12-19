using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace RolixSAEProject.Controllers
{
    public class PreferencesController : Controller
    {
        [HttpGet]
        public IActionResult SetCurrency(string currency, string? returnUrl)
        {
            var normalized = NormalizeCurrency(currency);

            Response.Cookies.Append("Currency", normalized, new CookieOptions
            {
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private static string NormalizeCurrency(string currency)
        {
            return currency switch
            {
                "CHF" => "CHF",
                "USD" => "USD",
                _ => "EUR"
            };
        }
    }
}
