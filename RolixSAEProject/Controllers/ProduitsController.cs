using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using RolixSAEProject.Models;
using RolixSAEProject.Services;
using System.Globalization;

namespace RolixSAEProject.Controllers
{
    public class ProduitsController : Controller
    {
        private readonly DataverseService _dataverseService;

        public ProduitsController(DataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        // GET: /Produits
        public IActionResult Index(string? search, string? categorie, string? collection, string? genre, string? sort)
        {
            var currency = ResolveCurrency();
            var dataverseLang = ResolveDataverseLanguage(); // ✅ NEW

            // ✅ NEW : on passe la langue à Dataverse (méthode avec param optionnel côté service)
            var produits = _dataverseService.GetProduitsRolix(dataverseLang);

            // Recherche
            if (!string.IsNullOrWhiteSpace(search))
            {
                produits = produits
                    .Where(p => p.Nom != null &&
                                p.Nom.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Filtre Catégorie (Edition limitée / Collection Sport)
            if (!string.IsNullOrEmpty(categorie) && categorie != "all")
            {
                produits = produits
                    .Where(p => p.Categorie == categorie)
                    .ToList();
            }

            // Filtre Collection (Classic / Pro)
            if (!string.IsNullOrEmpty(collection) && collection != "all")
            {
                produits = produits
                    .Where(p => p.Collection == collection)
                    .ToList();
            }

            // Filtre Genre (Femme / Homme / Unisex) -> en FR ou EN selon Dataverse
            if (!string.IsNullOrEmpty(genre) && genre != "all")
            {
                produits = produits
                    .Where(p => p.Genre == genre)
                    .ToList();
            }


            // ⬆⬇ Tri prix
            produits = sort switch
            {
                "price_desc" => produits.OrderByDescending(p => p.GetPrice(currency)).ToList(),
                "price_asc" => produits.OrderBy(p => p.GetPrice(currency)).ToList(),
                _ => produits.OrderBy(p => p.Nom).ToList()
            };

            // Listes pour les <select>
            ViewBag.Categories = produits
             .Select(p => p.Categorie)
             .Where(c => !string.IsNullOrWhiteSpace(c))
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .OrderBy(c => c)
             .ToList();

            ViewBag.Collections = produits
            .Select(p => p.Collection)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();


            // ✅ NEW : plus en dur -> basé sur les labels renvoyés par Dataverse (donc FR/EN automatique)
            ViewBag.Genres = produits
                .Select(p => p.Genre)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategorie = categorie;
            ViewBag.CurrentCollection = collection;
            ViewBag.CurrentGenre = genre;
            ViewBag.CurrentSort = sort;
            ViewBag.CurrentCurrency = currency;

            return View(produits);
        }

        // GET: /Produits/Details/1
        public IActionResult Details(int id)
        {
            var currency = ResolveCurrency();
            var dataverseLang = ResolveDataverseLanguage(); // ✅ NEW

            // ✅ NEW : langue passée à Dataverse
            var produit = _dataverseService.GetProduitRolixById(id, dataverseLang);

            if (produit == null)
            {
                return NotFound();
            }

            ViewBag.CurrentCurrency = currency;
            return View(produit);
        }

        private string ResolveCurrency()
        {
            var selected = Request.Cookies["Currency"];

            return selected switch
            {
                "CHF" => "CHF",
                "USD" => "USD",
                _ => "EUR"
            };
        }

        // ✅ NEW : on aligne la langue Dataverse sur la langue UI du site
        private string ResolveDataverseLanguage()
        {
            var uiCulture =
                HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture?.UICulture
                ?? CultureInfo.CurrentUICulture;

            return uiCulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase)
                ? "en-US"
                : "fr-FR";
        }
    }
}
