using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;

namespace RolixSAEProject.Controllers
{
    public class ContactController : Controller
    {
        private readonly DataverseService _dataverseService;

        public ContactController(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // On précise "Contact" car ton fichier ne s'appelle pas Index.cshtml
            return View("Contact");
        }

        [HttpPost]
        public async Task<IActionResult> Submit(ContactForm model)
        {
            if (ModelState.IsValid)
            {
                var leadData = new Dictionary<string, object>
                {
                    { "firstname", model.FirstName ?? "" },
                    { "lastname", model.LastName ?? "" },
                    { "emailaddress1", model.EMailAddress1 ?? "" },
                    { "subject", model.Message ?? "Nouveau message Web" }, // Rubrique
                    { "description", model.Description ?? "" },
                    { "leadsourcecode", 8 } // Source Web
                };

                // CORRECTION : "lead" sans le "s" pour Dataverse
                await _dataverseService.CreateEntity("lead", leadData);

                return RedirectToAction("Index", "Home");
            }
            // En cas d'erreur, on réaffiche bien la vue Contact
            return View("Contact", model);
        }
    }
}