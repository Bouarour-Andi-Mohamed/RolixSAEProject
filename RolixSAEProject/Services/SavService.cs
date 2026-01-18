using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class SavService
    {
        private readonly ServiceClient _client;

        // ✅ Table + champs EXACTS
        private const string SavTable = "crda6_sav";
        private const string ColClientSav = "crda6_clientsav";                  // lookup -> account
        private const string ColCommandeSav = "crda6_commandesav";              // lookup -> commandes
        private const string ColDescriptionProbleme = "crda6_descriptionprobleme"; // texte
        private const string ColProduitSav = "crda6_produitsav";                // lookup -> product

        // ✅ Change si besoin (si ta table commandes n'est pas salesorder)
        private const string OrderEntityLogicalName = "salesorder";

        // champs utiles pour l'affichage
        private const string ColCreatedOn = "createdon";
        private const string ColState = "statecode";
        private const string ColStatus = "statuscode";

        public SavService(ServiceClient client)
        {
            _client = client;
        }

        // ✅ Création d'une demande SAV
        public Guid CreateSav(Guid accountId, Guid orderId, Guid productId, string description)
        {
            if (_client?.IsReady != true)
                throw new InvalidOperationException("Dataverse client not ready");

            if (accountId == Guid.Empty) throw new Exception("Compte invalide.");
            if (orderId == Guid.Empty) throw new Exception("Commande invalide.");
            if (productId == Guid.Empty) throw new Exception("Produit invalide.");
            if (string.IsNullOrWhiteSpace(description)) throw new Exception("Description manquante.");

            var sav = new Entity(SavTable);

            sav[ColClientSav] = new EntityReference("account", accountId);
            sav[ColCommandeSav] = new EntityReference(OrderEntityLogicalName, orderId);
            sav[ColProduitSav] = new EntityReference("product", productId);
            sav[ColDescriptionProbleme] = description.Trim();

            var savId = _client.Create(sav);
            return savId;
        }

        // ✅ Liste des demandes SAV du compte (pour l'onglet "Mes demandes SAV")
        public List<SavRequestItem> GetSavRequestsByAccount(Guid accountId, int top = 50)
        {
            if (_client?.IsReady != true) return new List<SavRequestItem>();
            if (accountId == Guid.Empty) return new List<SavRequestItem>();

            var q = new QueryExpression(SavTable)
            {
                ColumnSet = new ColumnSet(
                    ColCreatedOn,
                    ColDescriptionProbleme,
                    ColCommandeSav,
                    ColProduitSav,
                    ColState,
                    ColStatus
                ),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ColClientSav, ConditionOperator.Equal, accountId)
                    }
                },
                TopCount = top
            };

            q.AddOrder(ColCreatedOn, OrderType.Descending);

            var rows = _client.RetrieveMultiple(q).Entities;

            var list = new List<SavRequestItem>();

            foreach (var e in rows)
            {
                var orderRef = e.GetAttributeValue<EntityReference>(ColCommandeSav);
                var prodRef = e.GetAttributeValue<EntityReference>(ColProduitSav);

                e.FormattedValues.TryGetValue(ColState, out var stateLabel);
                e.FormattedValues.TryGetValue(ColStatus, out var statusLabel);

                list.Add(new SavRequestItem
                {
                    SavId = e.Id,
                    CreatedOn = e.GetAttributeValue<DateTime?>(ColCreatedOn),
                    DescriptionProbleme = e.GetAttributeValue<string>(ColDescriptionProbleme) ?? "",

                    OrderId = orderRef?.Id,
                    OrderName = orderRef?.Name,

                    ProductId = prodRef?.Id,
                    ProductName = prodRef?.Name,

                    StateLabel = stateLabel ?? "",
                    StatusLabel = statusLabel ?? ""
                });
            }

            return list;
        }
    }
}
