using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;
using System;
using System.Globalization;

namespace RolixSAEProject.Controllers
{
    public class DevisController : Controller
    {
        private readonly QuoteService _quoteService;
        private readonly DataverseService _dataverseService;

        public DevisController(QuoteService quoteService, DataverseService dataverseService)
        {
            _quoteService = quoteService;
            _dataverseService = dataverseService;
        }

        // ✅ Devise = cookie (source officielle)
        private string GetSiteCurrency()
        {
            var c = Request.Cookies.TryGetValue("Currency", out var cookieCur) ? cookieCur : null;
            c = (c ?? "").Trim().ToUpperInvariant();

            return c switch
            {
                "CHF" => "CHF",
                "USD" => "USD",
                _ => "EUR"
            };
        }

        // Lang pour GetProduitsRolix
        private string GetDataverseLanguage()
        {
            // ta méthode ResolveLcid check "en" => 1033
            var ui = CultureInfo.CurrentUICulture?.Name ?? "fr-FR";
            return ui.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en-US" : "fr-FR";
        }

        [HttpGet]
        public IActionResult Demande(int id, string productDataverseId)
        {
            var accountId = HttpContext.Session.GetString("account_id");
            if (string.IsNullOrWhiteSpace(accountId))
            {
                var returnUrl = Url.Action("Demande", "Devis", new { id, productDataverseId }) ?? "/Account";
                return RedirectToAction("Login", "Auth", new { returnUrl });
            }

            var currency = GetSiteCurrency();
            var lang = GetDataverseLanguage();

            // Produit pour affichage (on ne casse pas ton mapping existant)
            var product = _dataverseService.GetProduitRolixById(id, lang);

            // GUID Dataverse: on prend celui du param, sinon celui du produit
            Guid dvId = Guid.Empty;
            if (!Guid.TryParse(productDataverseId, out dvId) && product != null)
                dvId = product.ProductDataverseId;

            if (dvId == Guid.Empty)
            {
                TempData["Error"] = "Produit invalide.";
                return RedirectToAction("Index", "Produits");
            }

            var vm = new QuoteRequestViewModel
            {
                ProductLocalId = id,
                ProductDataverseId = dvId,
                Currency = currency,
                Product = product,
                Quantity = 1,
                ContactPreference = "Email"
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Demande(QuoteRequestViewModel model)
        {
            var accountIdStr = HttpContext.Session.GetString("account_id");
            if (string.IsNullOrWhiteSpace(accountIdStr))
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });

            if (!Guid.TryParse(accountIdStr, out var accountId))
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });

            // ✅ Devise = cookie (pas le form)
            model.Currency = GetSiteCurrency();

            if (model.ProductDataverseId == Guid.Empty)
            {
                TempData["Error"] = "Produit invalide.";
                return RedirectToAction("Index", "Produits");
            }

            // Validation simple côté serveur
            if (!ModelState.IsValid)
            {
                // Recharge produit pour l'affichage
                var lang = GetDataverseLanguage();
                model.Product = _dataverseService.GetProduitRolixById(model.ProductLocalId, lang);
                return View(model);
            }

            try
            {
                // ✅ Pas de remise => on envoie 0
                var result = _quoteService.CreateQuoteWithLine(
                    accountId: accountId,
                    productId: model.ProductDataverseId,
                    quantity: model.Quantity,
                    manualDiscountAmount: 0m,
                    currency: model.Currency
                );

                // Bonus utile: stocker la raison/contact dans le devis (champ description)
                // Sans casser ta création: on fait juste un update après.
                var info =
                    $"Demande de devis (site web)\n" +
                    $"Quantité: {model.Quantity}\n" +
                    $"Raison: {model.Reason}\n" +
                    (string.IsNullOrWhiteSpace(model.Usage) ? "" : $"Usage: {model.Usage}\n") +
                    $"Préférence contact: {model.ContactPreference}\n" +
                    (string.IsNullOrWhiteSpace(model.ContactEmail) ? "" : $"Email: {model.ContactEmail}\n") +
                    (string.IsNullOrWhiteSpace(model.ContactPhone) ? "" : $"Téléphone: {model.ContactPhone}\n") +
                    (string.IsNullOrWhiteSpace(model.Notes) ? "" : $"Notes: {model.Notes}\n") +
                    $"Devise site: {model.Currency}\n";

                _quoteService.UpdateQuoteDescription(result.QuoteId, info);

                TempData["QuoteDebug"] = result.Debug;
                return RedirectToAction("Confirmation", new { id = result.QuoteId.ToString() });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Demande", new { id = model.ProductLocalId, productDataverseId = model.ProductDataverseId.ToString() });
            }
        }

        [HttpGet]
        public IActionResult Confirmation(string id)
        {
            ViewBag.QuoteId = id;
            ViewBag.Debug = TempData["QuoteDebug"] as string;
            return View();
        }
    }
}
