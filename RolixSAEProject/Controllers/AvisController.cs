using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;

namespace RolixSAEProject.Controllers
{
    public class AvisController : Controller
    {
        // Affiche la page pour saisir l'avis
        [HttpGet]
        public IActionResult Create(string orderId)
        {
            // On lie l'avis à la commande cliquée
            var model = new AvisViewModel { OrderId = orderId };
            return View(model);
        }

        // Action qui reçoit les données du formulaire (Note et Commentaire)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AvisViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Ici se fera plus tard l'envoi vers ta table Dataverse
                return RedirectToAction("Index", "Account");
            }
            return View(model);
        }
    }
}