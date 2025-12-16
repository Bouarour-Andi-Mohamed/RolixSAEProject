using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace RolixSAEProject.Controllers
{
    public class PreferencesController : Controller
    {
        [HttpGet]
        public IActionResult SetCurrency(string currency, string? returnUrl = null)
        {
            currency = (currency ?? "EUR").ToUpperInvariant();
            if (currency != "EUR" && currency != "CHF" && currency != "USD")
                currency = "EUR";

            Response.Cookies.Append("Currency", currency, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/",
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Produits");
        }
    }
}
