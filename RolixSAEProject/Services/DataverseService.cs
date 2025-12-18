using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class DataverseService
    {
        private readonly ServiceClient _client;

        // Cache: (entity|attribute|lcid) -> (value -> label)
        private readonly ConcurrentDictionary<string, IReadOnlyDictionary<int, string>> _labelsCache
            = new ConcurrentDictionary<string, IReadOnlyDictionary<int, string>>();

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

        // ---------------------------
        // Public overloads (compat)
        // ---------------------------
        public List<Produit> GetProduitsRolix() => GetProduitsRolix("fr-FR");

        public Produit? GetProduitRolixById(int id) => GetProduitRolixById(id, "fr-FR");

        // ---------------------------
        // Lang helpers
        // ---------------------------
        private static int ResolveLcid(string? dataverseLanguage)
        {
            if (string.IsNullOrWhiteSpace(dataverseLanguage))
                return 1036;

            var l = dataverseLanguage.Trim().ToLowerInvariant();
            return l.StartsWith("en") ? 1033 : 1036;
        }

        // ---------------------------
        // OptionSet labels (metadata)
        // ---------------------------
        private IReadOnlyDictionary<int, string> GetOptionSetLabels(string entityLogicalName, string attributeLogicalName, int lcid)
        {
            var cacheKey = $"{entityLogicalName}|{attributeLogicalName}|{lcid}";
            if (_labelsCache.TryGetValue(cacheKey, out var cached))
                return cached;

            if (Client?.IsReady != true)
                return new Dictionary<int, string>();

            try
            {
                var req = new RetrieveAttributeRequest
                {
                    EntityLogicalName = entityLogicalName,
                    LogicalName = attributeLogicalName,
                    RetrieveAsIfPublished = true
                };

                var resp = (RetrieveAttributeResponse)Client.Execute(req);
                var meta = resp.AttributeMetadata;

                // Choice / Picklist / MultiSelect -> EnumAttributeMetadata
                if (meta is not EnumAttributeMetadata enumMeta || enumMeta.OptionSet == null)
                    return new Dictionary<int, string>();

                var dict = new Dictionary<int, string>();
                foreach (var opt in enumMeta.OptionSet.Options ?? Enumerable.Empty<OptionMetadata>())
                {
                    if (!opt.Value.HasValue)
                        continue;

                    var label =
                        opt.Label?.LocalizedLabels?.FirstOrDefault(x => x.LanguageCode == lcid)?.Label
                        ?? opt.Label?.UserLocalizedLabel?.Label
                        ?? opt.Label?.LocalizedLabels?.FirstOrDefault()?.Label
                        ?? string.Empty;

                    dict[opt.Value.Value] = label;
                }

                _labelsCache[cacheKey] = dict;
                return dict;
            }
            catch
            {
                return new Dictionary<int, string>();
            }
        }

        private string GetChoiceLabel(string entityLogicalName, string attributeLogicalName, int? optionValue, int lcid, string fallback = "")
        {
            if (!optionValue.HasValue)
                return fallback;

            var map = GetOptionSetLabels(entityLogicalName, attributeLogicalName, lcid);
            return map.TryGetValue(optionValue.Value, out var label) && !string.IsNullOrWhiteSpace(label)
                ? label
                : fallback;
        }

        // ---------------------------
        // Pricing helpers (unchanged)
        // ---------------------------
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
        /// Retourne la liste des produits dont le parent est "Rolix".
        /// dataverseLanguage: "fr-FR" ou "en-US"
        /// </summary>
        public List<Produit> GetProduitsRolix(string dataverseLanguage)
        {
            if (Client?.IsReady != true)
            {
                return new List<Produit>();
            }

            var lcid = ResolveLcid(dataverseLanguage);

            // Nom logique table Produit
            const string productTable = "product";

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

            var productQuery = new QueryExpression(productTable)
            {
                ColumnSet = new ColumnSet(
                    "name",
                    "parentproductid",
                    "description",
                    "crda6_descriptionen",
                    "crda6_categorie",
                    "crda6_collection",
                    "crda6_genre",
                    "crda6_imageurl",
                    "crda6_mouvement",
                    "crda6_calibre",
                    "crda6_materiau",
                    "crda6_bracelet",
                    "crda6_tailleduboitiermm",
                    "crda6_etancheitem",
                    "crda6_verre",
                    "crda6_loupecyclope"
                )
            };

            var productResult = Client.RetrieveMultiple(productQuery);

            var produitsEntities = productResult.Entities
                .Where(e =>
                {
                    if (!e.Contains("parentproductid"))
                        return false;

                    var parent = e.GetAttributeValue<EntityReference>("parentproductid");
                    return parent != null &&
                           !string.IsNullOrEmpty(parent.Name) &&
                           parent.Name == "Rolix";
                })
                .ToList();

            var produits = produitsEntities
                .Select((e, index) =>
                {
                    // Catégorie (laisser tel quel pour ne pas casser tes filtres en dur côté Controller)
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

                    // Collection (laisser tel quel pour ne pas casser tes filtres en dur côté Controller)
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

                    // ✅ Genre (TRAD)
                    string genreTexte = string.Empty;
                    var genOsv = e.GetAttributeValue<OptionSetValue>("crda6_genre");
                    if (genOsv != null)
                    {
                        // fallback FR actuel (zéro risque)
                        var fallbackFr = genOsv.Value switch
                        {
                            0 => "Femme",
                            1 => "Homme",
                            2 => "Unisex",
                            _ => string.Empty
                        };

                        // si EN demandé -> label Dataverse (Male/Female/Unisex)
                        genreTexte = (lcid == 1033)
                            ? GetChoiceLabel(productTable, "crda6_genre", genOsv.Value, lcid, fallbackFr)
                            : fallbackFr;
                    }

                    // Description
                    // Description (FR = description, EN = crda6_descriptionen)
                    var descriptionFrRaw = e.GetAttributeValue<string>("description") ?? string.Empty;
                    var descriptionEnRaw = e.GetAttributeValue<string>("crda6_descriptionen") ?? string.Empty;

                    // Ce que l'UI doit afficher sans changer tes vues :
                    // - si site en EN -> on affiche EN (sinon fallback FR)
                    // - si site en FR -> on affiche FR (sinon fallback EN)
                    var descriptionUi =
                        (lcid == 1033)
                            ? (!string.IsNullOrWhiteSpace(descriptionEnRaw) ? descriptionEnRaw : descriptionFrRaw)
                            : (!string.IsNullOrWhiteSpace(descriptionFrRaw) ? descriptionFrRaw : descriptionEnRaw);

                    // On garde quand même les 2 champs remplis proprement pour le reste de l'app
                    var descriptionFr = !string.IsNullOrWhiteSpace(descriptionFrRaw) ? descriptionFrRaw : descriptionEnRaw;
                    var descriptionEn = !string.IsNullOrWhiteSpace(descriptionEnRaw) ? descriptionEnRaw : descriptionFrRaw;

                    decimal prixEUR = TryGetPrice(priceByCurrency, "EUR", e.Id);
                    decimal prixCHF = TryGetPrice(priceByCurrency, "CHF", e.Id);
                    decimal prixUSD = TryGetPrice(priceByCurrency, "USD", e.Id);

                    // ✅ Choices affichés (TRAD si EN, sinon ton comportement actuel)
                    var mouvementOsv = e.GetAttributeValue<OptionSetValue>("crda6_mouvement");
                    var materiauOsv = e.GetAttributeValue<OptionSetValue>("crda6_materiau");
                    var braceletOsv = e.GetAttributeValue<OptionSetValue>("crda6_bracelet");
                    var verreOsv = e.GetAttributeValue<OptionSetValue>("crda6_verre");

                    e.FormattedValues.TryGetValue("crda6_mouvement", out var mouvementLabelFR);
                    e.FormattedValues.TryGetValue("crda6_materiau", out var materiauLabelFR);
                    e.FormattedValues.TryGetValue("crda6_bracelet", out var braceletLabelFR);
                    e.FormattedValues.TryGetValue("crda6_verre", out var verreLabelFR);

                    var mouvement = (lcid == 1033)
                        ? GetChoiceLabel(productTable, "crda6_mouvement", mouvementOsv?.Value, lcid, mouvementLabelFR ?? string.Empty)
                        : (mouvementLabelFR ?? string.Empty);

                    var materiau = (lcid == 1033)
                        ? GetChoiceLabel(productTable, "crda6_materiau", materiauOsv?.Value, lcid, materiauLabelFR ?? string.Empty)
                        : (materiauLabelFR ?? string.Empty);

                    var bracelet = (lcid == 1033)
                        ? GetChoiceLabel(productTable, "crda6_bracelet", braceletOsv?.Value, lcid, braceletLabelFR ?? string.Empty)
                        : (braceletLabelFR ?? string.Empty);

                    var verre = (lcid == 1033)
                        ? GetChoiceLabel(productTable, "crda6_verre", verreOsv?.Value, lcid, verreLabelFR ?? string.Empty)
                        : (verreLabelFR ?? string.Empty);

                    // Texte / int / bool
                    var calibre = e.GetAttributeValue<string>("crda6_calibre") ?? string.Empty;
                    var etancheiteM = e.GetAttributeValue<string>("crda6_etancheitem") ?? string.Empty;
                    var tailleBoitierMm = e.GetAttributeValue<int?>("crda6_tailleduboitiermm") ?? 0;
                    var loupeCyclope = e.GetAttributeValue<bool?>("crda6_loupecyclope") ?? false;

                    return new Produit
                    {
                        Id = index + 1,
                        Nom = e.GetAttributeValue<string>("name") ?? string.Empty,
                        DescriptionFR = descriptionUi,
                        DescriptionEN = descriptionEn,


                        ImageUrl = e.GetAttributeValue<string>("crda6_imageurl") ?? string.Empty,

                        PrixEUR = prixEUR,
                        PrixCHF = prixCHF,
                        PrixUSD = prixUSD,

                        Categorie = categorieTexte,
                        Collection = collectionTexte,

                        // ✅ TRAD
                        Genre = genreTexte,

                        // ✅ TRAD (si EN demandé)
                        Mouvement = mouvement,
                        Calibre = calibre,
                        Materiau = materiau,
                        Bracelet = bracelet,
                        TailleBoitierMm = tailleBoitierMm,
                        EtancheiteM = etancheiteM,
                        Verre = verre,
                        LoupeCyclope = loupeCyclope,
                    };
                })
                .ToList();

            return produits;
        }

        /// <summary>
        /// Récupère un produit par Id (index dans la liste Rolix).
        /// </summary>
        public Produit? GetProduitRolixById(int id, string dataverseLanguage)
        {
            var produits = GetProduitsRolix(dataverseLanguage);
            return produits.FirstOrDefault(p => p.Id == id);
        }
    }
}




