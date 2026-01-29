using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk;
using RolixSAEProject.Models;
using RolixSAEProject.Services;
// Assure-toi que le namespace ci-dessous est le bon pour accéder à ton service
// using RolixSAEProject.Services; 

namespace RolixSAEProject.Controllers
{
    public class AvisController : Controller
    {
        // 1. Déclaration du service
        private readonly DataverseService _dataverseService;

        // 2. Constructeur (C'est ici que l'erreur arrive souvent si c'est mal écrit)
        public AvisController(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        [HttpGet]
        public IActionResult Create(string orderId)
        {
            if (string.IsNullOrEmpty(orderId)) return RedirectToAction("Index", "Account");
            var model = new AvisViewModel { OrderId = orderId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AvisViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Préparation des données avec les noms techniques de tes colonnes Power Apps
                    var data = new Dictionary<string, object>
                    {
                        { "crda6_name", $"Avis Commande {model.OrderId}" },
                        { "crda6_note", model.Note },
                        { "crda6_commentaire", model.Commentaire },
                        { "crda6_commandeassociee", new EntityReference("salesorder", new Guid(model.OrderId)) }
                    };

                    // ENVOI À DATAVERSE
                    await _dataverseService.CreateEntity("crda6_avisclient", data);

                    TempData["SuccessMessage"] = "L'avis a été envoyé avec succès !";
                    return RedirectToAction("Index", "Account");
                }
                catch (Exception ex)
                {
                    // Debug : Affiche l'erreur précise si l'envoi échoue
                    ModelState.AddModelError("", "Erreur d'envoi : " + ex.Message);
                }
            }
            return View(model);
        }
    }
}