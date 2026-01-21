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
        private readonly QuoteService _quoteService;

        public AccountController(
            CustomerAccountService accountService,
            OrderService orderService,
            SavService savService,
            QuoteService quoteService)
        {
            _accountService = accountService;
            _orderService = orderService;
            _savService = savService;
            _quoteService = quoteService;
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

            var orders = new List<OrderSummary>();
            var savs = new List<SavRequestItem>();
            var quotes = new List<QuoteSummary>();

            if (Guid.TryParse(accountIdStr, out var accountId))
            {
                orders = _orderService.GetOrdersForAccount(accountId, top: 50);
                savs = _savService.GetSavRequestsByAccount(accountId, top: 50);
                quotes = _quoteService.GetQuotesForAccount(accountId, top: 50);
            }

            ViewBag.SavRequests = savs;
            ViewBag.Quotes = quotes;

            var vm = new AccountPageViewModel
            {
                Profile = profile,
                Orders = orders
            };

            return View("~/Views/Auth/Account.cshtml", vm);
        }
    }
}
