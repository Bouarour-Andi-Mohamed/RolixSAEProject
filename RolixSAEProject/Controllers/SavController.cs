using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;
using System;

namespace RolixSAEProject.Controllers
{
    public class SavController : Controller
    {
        private readonly OrderService _orderService;
        private readonly SavService _savService;

        public SavController(OrderService orderService, SavService savService)
        {
            _orderService = orderService;
            _savService = savService;
        }

        // GET: /Sav/Create?orderId=GUID
        [HttpGet]
        public IActionResult Create(string orderId)
        {
            var accountIdStr = HttpContext.Session.GetString("account_id");
            if (string.IsNullOrWhiteSpace(accountIdStr))
                return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path + Request.QueryString });

            if (!Guid.TryParse(orderId, out var oid) || oid == Guid.Empty)
                return RedirectToAction("Index", "Account");

            var products = _orderService.GetProductsForOrder(oid);

            var vm = new SavCreateViewModel
            {
                OrderId = oid,
                Products = products
            };

            return View("~/Views/Sav/Create.cshtml", vm);
        }

        // POST: /Sav/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SavCreateViewModel model)
        {
            var accountIdStr = HttpContext.Session.GetString("account_id");
            if (string.IsNullOrWhiteSpace(accountIdStr))
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });

            if (!Guid.TryParse(accountIdStr, out var accountId) || accountId == Guid.Empty)
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });

            // recharge produits si erreur validation (sinon la dropdown se vide)
            if (!ModelState.IsValid)
            {
                model.Products = _orderService.GetProductsForOrder(model.OrderId);
                return View("~/Views/Sav/Create.cshtml", model);
            }

            try
            {
                var savId = _savService.CreateSav(
                    accountId: accountId,
                    orderId: model.OrderId,
                    productId: model.ProductId,
                    description: model.ProblemDescription
                );

                return RedirectToAction("Confirmation", new { id = savId.ToString() });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.Products = _orderService.GetProductsForOrder(model.OrderId);
                return View("~/Views/Sav/Create.cshtml", model);
            }
        }

        // GET: /Sav/Confirmation?id=GUID
        [HttpGet]
        public IActionResult Confirmation(string id)
        {
            ViewBag.SavId = id;
            return View("~/Views/Sav/Confirmation.cshtml");
        }
    }
}
