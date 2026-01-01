using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class QuoteService
    {
        private readonly ServiceClient _client;

        public QuoteService(ServiceClient client)
        {
            _client = client;
        }

        public class QuoteCreationResult
        {
            public Guid QuoteId { get; set; }
            public string Debug { get; set; } = "";
        }

        // ✅ Normalise ce que le site peut envoyer (EUR/€/"euro"/"franc suisse"/etc.)
        private static string NormalizeCurrency(string currency)
        {
            currency = (currency ?? "").Trim();

            if (currency.Length == 0) return "EUR";

            var c = currency.ToUpperInvariant();

            // cas où le site envoie des noms
            if (c.Contains("EURO") || c == "€") return "EUR";
            if (c.Contains("FRANC") || c.Contains("SUISSE")) return "CHF";
            if (c.Contains("DOLLAR") || c == "$") return "USD";

            // cas standard
            if (c == "EUR" || c == "CHF" || c == "USD") return c;

            // fallback
            return "EUR";
        }

        // ✅ mapping EXACT des price lists que tu as dans Dataverse
        private static string ResolvePriceListName(string currency)
        {
            currency = NormalizeCurrency(currency);

            return currency switch
            {
                "CHF" => "Tarification en CHF",
                "USD" => "Tarification US",
                _ => "Tarifications en EUR"
            };
        }

        // ✅ retrouve transactioncurrencyid pour EUR/CHF/USD
        // Dans TON Dataverse tu as currencyname = "euro" / "franc suisse" / "US Dollar"
        private Guid? ResolveTransactionCurrencyId(string currency)
        {
            currency = NormalizeCurrency(currency);

            // 1) Essai standard : isocurrencycode = "EUR"/"CHF"/"USD"
            var q1 = new QueryExpression("transactioncurrency")
            {
                ColumnSet = new ColumnSet("transactioncurrencyid", "isocurrencycode", "currencyname"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("isocurrencycode", ConditionOperator.Equal, currency)
                    }
                },
                TopCount = 1
            };

            var byIso = _client.RetrieveMultiple(q1).Entities.FirstOrDefault();
            if (byIso != null) return byIso.Id;

            // 2) Fallback par currencyname (selon tes valeurs)
            var currencyName = currency switch
            {
                "CHF" => "franc suisse",
                "USD" => "US Dollar",
                _ => "euro"
            };

            var q2 = new QueryExpression("transactioncurrency")
            {
                ColumnSet = new ColumnSet("transactioncurrencyid", "currencyname"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("currencyname", ConditionOperator.Equal, currencyName)
                    }
                },
                TopCount = 1
            };

            return _client.RetrieveMultiple(q2).Entities.FirstOrDefault()?.Id;
        }

        // ✅ retrouve pricelevelid à partir du NOM EXACT de ta liste de prix
        private Guid? ResolvePriceLevelId(string priceListName)
        {
            var q = new QueryExpression("pricelevel")
            {
                ColumnSet = new ColumnSet("pricelevelid", "name"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, priceListName)
                    }
                },
                TopCount = 1
            };

            return _client.RetrieveMultiple(q).Entities.FirstOrDefault()?.Id;
        }

        // ✅ prix du produit selon la price list choisie
        private decimal ResolvePricePerUnit(Guid productId, Guid priceLevelId)
        {
            var q = new QueryExpression("productpricelevel")
            {
                ColumnSet = new ColumnSet("amount", "productid", "pricelevelid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("productid", ConditionOperator.Equal, productId),
                        new ConditionExpression("pricelevelid", ConditionOperator.Equal, priceLevelId),
                    }
                },
                TopCount = 1
            };

            var ppl = _client.RetrieveMultiple(q).Entities.FirstOrDefault();
            var money = ppl?.GetAttributeValue<Money>("amount");
            return money?.Value ?? 0m;
        }

        private (string name, Guid? defaultUomId) GetProductInfos(Guid productId)
        {
            var prod = _client.Retrieve("product", productId, new ColumnSet("name", "defaultuomid"));
            var name = prod.GetAttributeValue<string>("name") ?? "Produit";
            var uomRef = prod.GetAttributeValue<EntityReference>("defaultuomid");
            return (name, uomRef?.Id);
        }

        /// <summary>
        /// Crée un devis + une ligne devis depuis une page produit.
        /// IMPORTANT: statecode = 0 (Brouillon)
        /// + transactioncurrencyid = devise du site
        /// + pricelevelid = bonne tarification (EUR/CHF/USD)
        /// </summary>
        public QuoteCreationResult CreateQuoteWithLine(Guid accountId, Guid productId, decimal quantity, decimal manualDiscountAmount, string currency)
        {
            if (_client?.IsReady != true)
                throw new InvalidOperationException("Dataverse client not ready");

            if (quantity <= 0) quantity = 1;
            if (manualDiscountAmount < 0) manualDiscountAmount = 0;

            // ✅ Normalise devise venant du site
            currency = NormalizeCurrency(currency);

            var debug = "";
            debug += $"accountId={accountId}\nproductId={productId}\nquantity={quantity}\ndiscount={manualDiscountAmount}\ncurrency(normalized)={currency}\n";

            // ✅ 1) transactioncurrencyid (devise)
            var transactionCurrencyId = ResolveTransactionCurrencyId(currency)
                ?? throw new Exception($"Transaction currency introuvable pour: {currency} (euro/franc suisse/US Dollar)");

            debug += $"transactionCurrencyId={transactionCurrencyId}\n";

            // ✅ 2) pricelevelid (tarifs) selon devise
            var priceListName = ResolvePriceListName(currency);
            var priceLevelId = ResolvePriceLevelId(priceListName)
                ?? throw new Exception($"Price list introuvable: {priceListName}");

            debug += $"priceListName={priceListName}\npriceLevelId={priceLevelId}\n";

            // 3) Infos produit
            var (productName, defaultUomId) = GetProductInfos(productId);
            if (!defaultUomId.HasValue)
                throw new Exception("defaultuomid manquant sur le produit (obligatoire pour quotedetail).");

            debug += $"productName={productName}\ndefaultUomId={defaultUomId}\n";

            // 4) Créer le devis
            var quote = new Entity("quote");
            quote["name"] = $"Devis - {productName} - {DateTime.Now:yyyy-MM-dd HH:mm}";
            quote["customerid"] = new EntityReference("account", accountId);

            // ✅ IMPORTANT : ces 2 champs doivent correspondre à la devise du site
            quote["transactioncurrencyid"] = new EntityReference("transactioncurrency", transactionCurrencyId);
            quote["pricelevelid"] = new EntityReference("pricelevel", priceLevelId);

            // ✅ BROUILLON
            quote["statecode"] = new OptionSetValue(0);
            quote["statuscode"] = new OptionSetValue(1);

            var quoteId = _client.Create(quote);
            debug += $"quoteId={quoteId}\n";

            // sécurise l'état
            try
            {
                var setState = new SetStateRequest
                {
                    EntityMoniker = new EntityReference("quote", quoteId),
                    State = new OptionSetValue(0),
                    Status = new OptionSetValue(1)
                };
                _client.Execute(setState);
                debug += "SetState OK => statecode=0 statuscode=1\n";
            }
            catch (Exception ex)
            {
                debug += $"SetState FAILED: {ex.Message}\n";
            }

            // 5) Créer la ligne avec le prix de la bonne price list
            var pricePerUnit = ResolvePricePerUnit(productId, priceLevelId);
            debug += $"pricePerUnit(from productpricelevel)={pricePerUnit}\n";

            var line = new Entity("quotedetail");
            line["quoteid"] = new EntityReference("quote", quoteId);
            line["productid"] = new EntityReference("product", productId);
            line["uomid"] = new EntityReference("uom", defaultUomId.Value);
            line["quantity"] = quantity;

            line["ispriceoverridden"] = true;
            line["priceperunit"] = new Money(pricePerUnit);

            if (manualDiscountAmount > 0)
                line["manualdiscountamount"] = new Money(manualDiscountAmount);

            var lineId = _client.Create(line);
            debug += $"quotedetailId={lineId}\n";

            return new QuoteCreationResult
            {
                QuoteId = quoteId,
                Debug = debug
            };
        }
    }
}
