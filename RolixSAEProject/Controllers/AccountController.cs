using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;
using System;
using System.Collections.Generic;

namespace RolixSAEProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly CustomerAccountService _accountService;
        private readonly OrderService _orderService;
        private readonly SavService _savService;

        public AccountController(CustomerAccountService accountService, OrderService orderService, SavService savService)
        {
            _accountService = accountService;
            _orderService = orderService;
            _savService = savService;
        }

        // ✅ URL: /Account
        public IActionResult Index()
        {
            var accountIdStr = HttpContext.Session.GetString("account_id");

            if (string.IsNullOrWhiteSpace(accountIdStr))
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });

            var profile = _accountService.GetProfile(accountIdStr);
            if (profile == null)
            {
                HttpContext.Session.Remove("account_id");
                return RedirectToAction("Login", "Auth", new { returnUrl = "/Account" });
            }

            // ✅ commandes
            var orders = new List<OrderSummary>();

            // ✅ demandes SAV
            var savs = new List<SavRequestItem>();

            if (Guid.TryParse(accountIdStr, out var accountId))
            {
                orders = _orderService.GetOrdersForAccount(accountId, top: 30);
                savs = _savService.GetSavRequestsByAccount(accountId, top: 50);
            }

            // ✅ ViewModel (ton existant)
            var vm = new AccountPageViewModel
            {
                Profile = profile,
                Orders = orders
            };

            // ✅ On passe les SAV sans casser ton VM
            ViewBag.SavRequests = savs;

            return View("~/Views/Auth/Account.cshtml", vm);
        }
    }
}
