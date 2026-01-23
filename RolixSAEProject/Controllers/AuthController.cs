using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;
using System;
using System.Security.Cryptography;

namespace RolixSAEProject.Controllers
{
    public class AuthController : Controller
    {
        private readonly CustomerAccountService _accountService;
        private readonly IEmailSender _emailSender;

        public AuthController(CustomerAccountService accountService, IEmailSender emailSender)
        {
            _accountService = accountService;
            _emailSender = emailSender;
        }

        // =========================
        // LOGIN
        // =========================
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
            var profile = _accountService.ValidateLogin(identifiant, motDePasse);

            if (profile == null)
            {
                TempData["Error"] = "Identifiant ou mot de passe incorrect.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            HttpContext.Session.SetString("account_id", profile.AccountId);
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Account" : returnUrl);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("account_id");
            return RedirectToAction("Index", "Home");
        }

        // =========================
        // FORGOT PASSWORD (par identifiant -> emailaddress2)
        // =========================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            // Ton ForgotPasswordViewModel doit contenir: Identifiant (pas Email)
            // Si tu as encore "Email" dedans, change le viewmodel + la view.
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendResetCode(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View("ForgotPassword", model);

            // On lit l'identifiant saisi
            var identifiant = (model.Identifiant ?? "").Trim();
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                model.Error = "Identifiant requis.";
                return View("ForgotPassword", model);
            }

            // Récupère le compte via identifiant (champ crda6_identifiant) => emailaddress2
            var acc = _accountService.GetAccountByIdentifiant(identifiant);

            // Réponse "safe" : on ne dit pas si l'identifiant existe ou non
            // MAIS on ne peut pas continuer si email2 est vide
            if (acc == null || string.IsNullOrWhiteSpace(acc.Email2))
            {
                model.Info = "Si un compte existe pour cet identifiant, un code a été envoyé par email.";
                return View("ForgotPassword", model);
            }

            // Génère un code 6 chiffres
            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            // Stock en session (15 min)
            HttpContext.Session.SetString("fp_ident", identifiant);
            HttpContext.Session.SetString("fp_email", acc.Email2);
            HttpContext.Session.SetString("fp_code", code);
            HttpContext.Session.SetString("fp_exp", DateTime.UtcNow.AddMinutes(15).Ticks.ToString());

            var subject = "Rolix - Code de réinitialisation du mot de passe";
            var body =
                $"Bonjour,\n\n" +
                $"Identifiant : {identifiant}\n" +
                $"Voici votre code de réinitialisation : {code}\n" +
                $"Ce code est valable 15 minutes.\n\n" +
                $"Si vous n’êtes pas à l’origine de cette demande, ignorez cet email.\n";

            // IMPORTANT : ne pas avaler l'erreur sinon tu ne sais jamais pourquoi tu reçois rien
            try
            {
                _emailSender.Send(acc.Email2, subject, body);
            }
            catch (Exception ex)
            {
                model.Error = "Impossible d’envoyer l’email. Vérifie la configuration SMTP (Host/User/Pass/From).";
                model.Info = ex.Message; // en dev, utile pour voir le vrai problème
                return View("ForgotPassword", model);
            }

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            // On lit l'email depuis la session
            var email = HttpContext.Session.GetString("fp_email");
            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction("ForgotPassword");

            return View(new ResetPasswordViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            // email de session (source de vérité)
            var sessionEmail = HttpContext.Session.GetString("fp_email");
            var sessionCode = HttpContext.Session.GetString("fp_code");
            var expStr = HttpContext.Session.GetString("fp_exp");
            var identifiant = HttpContext.Session.GetString("fp_ident");

            if (string.IsNullOrWhiteSpace(sessionEmail) || string.IsNullOrWhiteSpace(sessionCode) || string.IsNullOrWhiteSpace(expStr))
            {
                model.Error = "Session expirée. Recommencez.";
                return View(model);
            }

            if (!long.TryParse(expStr, out var ticks) || new DateTime(ticks, DateTimeKind.Utc) < DateTime.UtcNow)
            {
                ClearResetSession();
                model.Error = "Code expiré. Recommencez.";
                return View(model);
            }

            // Vérif code
            if (!string.Equals((model.Code ?? "").Trim(), sessionCode, StringComparison.Ordinal))
            {
                model.Error = "Code incorrect.";
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            // On retrouve le compte par identifiant (recommandé)
            if (string.IsNullOrWhiteSpace(identifiant))
            {
                model.Error = "Session invalide. Recommencez.";
                return View(model);
            }

            var acc = _accountService.GetAccountByIdentifiant(identifiant);
            if (acc == null)
            {
                model.Error = "Compte introuvable.";
                return View(model);
            }

            _accountService.UpdatePassword(acc.AccountId, model.NewPassword);

            ClearResetSession();
            TempData["Success"] = "Mot de passe modifié. Vous pouvez vous connecter.";
            return RedirectToAction("Login");
        }

        private void ClearResetSession()
        {
            HttpContext.Session.Remove("fp_ident");
            HttpContext.Session.Remove("fp_email");
            HttpContext.Session.Remove("fp_code");
            HttpContext.Session.Remove("fp_exp");
        }
    }
}
