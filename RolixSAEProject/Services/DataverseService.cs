using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class DataverseService
    {
        private readonly ServiceClient _client;

        public DataverseService(IConfiguration configuration)
        {
            var url = configuration["Dataverse:Url"];

            var connStr =
                $"AuthType=OAuth;" +
                $"Url={url};" +
                $"AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;" +
                $"RedirectUri=http://localhost;" +
                $"LoginPrompt=Auto;";

            _client = new ServiceClient(connStr);
        }

        private ServiceClient Client => _client;

        private Guid? ResolvePriceListId(string priceListName)
        {
            var priceListQuery = new QueryExpression("pricelevel")
            {
                ColumnSet = new ColumnSet("pricelevelid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, priceListName)
                    }
                }
            };

            var priceListResult = Client.RetrieveMultiple(priceListQuery);
            if (priceListResult.Entities.Count > 0)
            {
                return priceListResult.Entities[0].Id;
            }

            return null;
        }

        private Dictionary<Guid, decimal> LoadPricesForList(Guid priceListId)
        {
            var prices = new Dictionary<Guid, decimal>();

            var productPriceQuery = new QueryExpression("productpricelevel")
            {
                ColumnSet = new ColumnSet("productid", "pricelevelid", "amount"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("pricelevelid", ConditionOperator.Equal, priceListId)
                    }
                }
            };

            var productPriceResult = Client.RetrieveMultiple(productPriceQuery);

            foreach (var pp in productPriceResult.Entities)
            {
                var productRef = pp.GetAttributeValue<EntityReference>("productid");
                var amount = pp.GetAttributeValue<Money>("amount");

                if (productRef != null && amount != null)
                {
                    prices[productRef.Id] = amount.Value;
                }
            }

            return prices;
        }

        private static decimal TryGetPrice(Dictionary<string, Dictionary<Guid, decimal>> priceByCurrency, string currency, Guid productId)
        {
            if (priceByCurrency.TryGetValue(currency, out var prices) && prices.TryGetValue(productId, out var amount))
            {
                return amount;
            }

            return 0m;
        }

        /// <summary>
        /// Retourne la liste des produits dont le parent est "Rolix",
        /// avec description et prix pour les différentes devises disponibles.
        /// </summary>
        public List<Produit> GetProduitsRolix()
        {
            if (Client?.IsReady != true)
            {
                return new List<Produit>();
            }

            // 1) Nom logique exact de ta table Produit
            const string productTable = "product"; // ex : "product" ou "crda6_produit"

            var priceLists = new Dictionary<string, string>
            {
                { "EUR", "Tarifications en EUR" },
                { "CHF", "Tarification en CHF" },
                { "USD", "Tarification US" }
            };

            var priceByCurrency = new Dictionary<string, Dictionary<Guid, decimal>>();

            foreach (var kvp in priceLists)
            {
                var priceListId = ResolvePriceListId(kvp.Value);
                priceByCurrency[kvp.Key] = priceListId.HasValue
                    ? LoadPricesForList(priceListId.Value)
                    : new Dictionary<Guid, decimal>();
            }

            // ------------------------------------------------------------
            // 3. Récupérer les produits
            // ------------------------------------------------------------
            var productQuery = new QueryExpression(productTable)
            {
                ColumnSet = new ColumnSet(
                    "name",
                    "parentproductid",
                    "description",
                    "crda6_categorie",
                    "crda6_collection",
                    "crda6_genre",
                    "crda6_imageurl"
                )
            };

            var productResult = Client.RetrieveMultiple(productQuery);

            // Garder uniquement les produits dont le parent = "Rolix"
            var produitsEntities = productResult.Entities
                .Where(e =>
                {
                    if (!e.Contains("parentproductid"))
                        return false;

                    var parent = e.GetAttributeValue<EntityReference>("parentproductid");
                    return parent != null &&
                           !string.IsNullOrEmpty(parent.Name) &&
                           parent.Name == "Rolix";   // ⚠️ 2) adapter si le parent s’appelle autrement
                })
                .ToList();

            // ------------------------------------------------------------
            // 4. Mapping vers notre modèle Produit
            // ------------------------------------------------------------
            var produits = produitsEntities
                .Select((e, index) =>
                {
                    // --- Catégorie : crda6_Categorie (Édition limitée / Collection Sport) ---
                    string categorieTexte = string.Empty;
                    var catOsv = e.GetAttributeValue<OptionSetValue>("crda6_categorie");
                    if (catOsv != null)
                    {
                        categorieTexte = catOsv.Value switch
                        {
                            0 => "Édition limitée",
                            1 => "Collection Sport",
                            _ => string.Empty
                        };
                    }

                    // --- Collection : crda6_Collection (Classic / Pro) ---
                    string collectionTexte = string.Empty;
                    var colOsv = e.GetAttributeValue<OptionSetValue>("crda6_collection");
                    if (colOsv != null)
                    {
                        collectionTexte = colOsv.Value switch
                        {
                            0 => "Classic",
                            1 => "Pro",
                            _ => string.Empty
                        };
                    }

                    // --- Genre : crda6_Genre (Femme / Homme / Unisex) ---
                    string genreTexte = string.Empty;
                    var genOsv = e.GetAttributeValue<OptionSetValue>("crda6_genre");
                    if (genOsv != null)
                    {
                        genreTexte = genOsv.Value switch
                        {
                            0 => "Femme",
                            1 => "Homme",
                            2 => "Unisex",
                            _ => string.Empty
                        };
                    }

                    // --- Description ---
                    string description = e.GetAttributeValue<string>("description") ?? string.Empty;

                    decimal prixEUR = TryGetPrice(priceByCurrency, "EUR", e.Id);
                    decimal prixCHF = TryGetPrice(priceByCurrency, "CHF", e.Id);
                    decimal prixUSD = TryGetPrice(priceByCurrency, "USD", e.Id);

                    return new Produit
                    {
                        Id = index + 1,
                        Nom = e.GetAttributeValue<string>("name") ?? string.Empty,
                        DescriptionFR = description,
                        ImageUrl = e.GetAttributeValue<string>("crda6_imageurl") ?? string.Empty,
                        PrixEUR = prixEUR,
                        PrixCHF = prixCHF,
                        PrixUSD = prixUSD,
                        Categorie = categorieTexte,
                        Collection = collectionTexte,
                        Genre = genreTexte
                    };
                })
                .ToList();

            return produits;
        }

        /// <summary>
        /// Récupère un produit par Id (index dans la liste Rolix).
        /// </summary>
        public Produit? GetProduitRolixById(int id)
        {
            var produits = GetProduitsRolix();
            return produits.FirstOrDefault(p => p.Id == id);
        }
    }
}
