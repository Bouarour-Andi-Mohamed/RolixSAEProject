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

        /// <summary>
        /// Retourne la liste des produits dont le parent est "Rolix",
        /// avec description et prix EUR (price list "Tarifications en France").
        /// </summary>
        public List<Produit> GetProduitsRolix()
        {
            if (Client?.IsReady != true)
            {
                return new List<Produit>();
            }

            // 1) Nom logique exact de ta table Produit
            const string productTable = "product"; // ex : "product" ou "crda6_produit"

            // ------------------------------------------------------------
            // 1. Récupérer l'ID de la price list "Tarifications en France"
            // ------------------------------------------------------------
            Guid? francePriceListId = null;

            var priceListQuery = new QueryExpression("pricelevel")
            {
                ColumnSet = new ColumnSet("pricelevelid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, "Tarifications en EUR")
                    }
                }
            };

            var priceListResult = Client.RetrieveMultiple(priceListQuery);
            if (priceListResult.Entities.Count > 0)
            {
                francePriceListId = priceListResult.Entities[0].Id;
            }

            // ------------------------------------------------------------
            // 2. Récupérer les montants de chaque produit pour cette price list
            // ------------------------------------------------------------
            var priceByProductId = new Dictionary<Guid, decimal>();

            if (francePriceListId.HasValue)
            {
                var productPriceQuery = new QueryExpression("productpricelevel")
                {
                    ColumnSet = new ColumnSet("productid", "pricelevelid", "amount"),
                    Criteria =
                    {
                        Conditions =
                        {
                            new ConditionExpression("pricelevelid", ConditionOperator.Equal, francePriceListId.Value)
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
                        priceByProductId[productRef.Id] = amount.Value;
                    }
                }
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

                    // --- Prix : depuis productpricelevel / Tarifications en France ---
                    decimal prix = 0m;
                    if (priceByProductId.TryGetValue(e.Id, out var prixTrouve))
                    {
                        prix = prixTrouve;
                    }

                    return new Produit
                    {
                        Id = index + 1,
                        Nom = e.GetAttributeValue<string>("name") ?? string.Empty,
                        DescriptionFR = description,
                        ImageUrl = e.GetAttributeValue<string>("crda6_imageurl") ?? string.Empty,
                        PrixEUR = prix,
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
