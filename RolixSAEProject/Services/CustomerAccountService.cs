using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class CustomerAccountService
    {
        private readonly ServiceClient _client;

        // Table + champs logiques
        private const string Table = "account";
        private const string ColAccountId = "accountid";
        private const string ColName = "name";
        private const string ColIdentifiant = "crda6_identifiant";
        private const string ColMotDePasse = "crda6_motdepasse";

        public CustomerAccountService(ServiceClient client)
        {
            _client = client;
        }

        // ✅ AJOUT: méthode attendue par AuthController
        public string? ValidateCredentials(string identifiant, string motDePasse)
        {
            var profile = ValidateLogin(identifiant, motDePasse);
            return profile?.AccountId;
        }

        /// <summary>
        /// Vérifie identifiant + mot de passe dans Dataverse.
        /// Retourne un profil si OK, sinon null.
        /// </summary>
        public AccountProfile? ValidateLogin(string identifiant, string motDePasse)
        {
            if (_client?.IsReady != true) return null;

            identifiant = (identifiant ?? "").Trim();
            motDePasse = (motDePasse ?? "").Trim();

            if (identifiant.Length == 0 || motDePasse.Length == 0)
                return null;

            var query = new QueryExpression(Table)
            {
                ColumnSet = new ColumnSet(ColAccountId, ColName, ColIdentifiant, ColMotDePasse),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ColIdentifiant, ConditionOperator.Equal, identifiant)
                    }
                },
                TopCount = 1
            };

            var entity = _client.RetrieveMultiple(query).Entities.FirstOrDefault();
            if (entity == null) return null;

            var storedPwd = entity.GetAttributeValue<string>(ColMotDePasse) ?? "";

            // ✅ Simulation simple: comparaison directe
            if (!string.Equals(storedPwd, motDePasse, StringComparison.Ordinal))
                return null;

            return new AccountProfile
            {
                AccountId = entity.Id.ToString(),
                NomCompte = entity.GetAttributeValue<string>(ColName) ?? "",
                Identifiant = entity.GetAttributeValue<string>(ColIdentifiant) ?? identifiant
            };
        }

        /// <summary>
        /// Récupère le profil à partir de l'id stocké en session.
        /// </summary>
        public AccountProfile? GetProfile(string accountId)
        {
            if (_client?.IsReady != true) return null;

            if (!Guid.TryParse(accountId, out var id))
                return null;

            Entity entity;
            try
            {
                entity = _client.Retrieve(Table, id, new ColumnSet(ColAccountId, ColName, ColIdentifiant));
            }
            catch
            {
                return null;
            }

            return new AccountProfile
            {
                AccountId = entity.Id.ToString(),
                NomCompte = entity.GetAttributeValue<string>(ColName) ?? "",
                Identifiant = entity.GetAttributeValue<string>(ColIdentifiant) ?? ""
            };
        }
    }
}
