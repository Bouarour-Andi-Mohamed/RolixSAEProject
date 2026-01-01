using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Services;
using RolixSAEProject.Helpers;
using System;

namespace RolixSAEProject.Controllers
{
    public class DevisController : Controller
    {
        private readonly QuoteService _quoteService;

        public DevisController(QuoteService quoteService)
        {
            _quoteService = quoteService;
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

            if (string.IsNullOrWhiteSpace(productDataverseId))
            {
                TempData["Error"] = "Produit introuvable (GUID Dataverse manquant).";
                return RedirectToAction("Index", "Produits");
            }

            ViewBag.ProductDataverseId = productDataverseId;
            return View();
        }

        // POST: /Devis/Demande
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Demande(string productDataverseId, decimal quantity, decimal manualDiscountAmount, string currency)
        {
            var accountIdStr = HttpContext.Session.GetString("account_id");
            if (string.IsNullOrWhiteSpace(accountIdStr))
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });
            }

            if (!Guid.TryParse(accountIdStr, out var accountId))
            {
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });
            }

            if (string.IsNullOrWhiteSpace(productDataverseId) || !Guid.TryParse(productDataverseId, out var productId))
            {
                ViewBag.ProductDataverseId = productDataverseId;
                TempData["Error"] = "Produit invalide (GUID Dataverse).";
                return View();
            }

            try
            {
                var result = _quoteService.CreateQuoteWithLine(
                    accountId: accountId,
                    productId: productId,
                    quantity: quantity,
                    manualDiscountAmount: manualDiscountAmount,
                    currency: currency
                );

                // ✅ message + debug pour la page confirmation
                TempData["QuoteMessage"] = "✅ La demande de devis est envoyée sur Power Apps (Dataverse).";
                TempData["QuoteDebug"] = result.Debug;

                return RedirectToAction("Confirmation", new { id = result.QuoteId.ToString() });
            }
            catch (Exception ex)
            {
                // ✅ Rester sur la page Demande, afficher l’erreur
                ViewBag.ProductDataverseId = productDataverseId;
                TempData["Error"] = "Erreur lors de la création du devis : " + ex.Message;
                return View();
            }
        }

        // GET: /Devis/Confirmation/{id}
        [HttpGet]
        public IActionResult Confirmation(string id)
        {
            ViewBag.QuoteId = id;
            ViewBag.Message = TempData["QuoteMessage"] as string;
            ViewBag.Debug = TempData["QuoteDebug"] as string;
            return View();
        }
    }
}
