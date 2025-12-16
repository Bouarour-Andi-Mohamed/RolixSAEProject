using Microsoft.AspNetCore.Mvc;
using RolixSAEProject.Models;
using RolixSAEProject.Services;

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
            var produits = _dataverseService.GetProduitsRolix();

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

            //Filtre Genre (Femme / Homme / Unisex)
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
            ViewBag.Categories = new List<string> { "Édition limitée", "Collection Sport" };
            ViewBag.Collections = new List<string> { "Classic", "Pro" };
            ViewBag.Genres = new List<string> { "Femme", "Homme", "Unisex" };

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
            var produit = _dataverseService.GetProduitRolixById(id);

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
    }
}
