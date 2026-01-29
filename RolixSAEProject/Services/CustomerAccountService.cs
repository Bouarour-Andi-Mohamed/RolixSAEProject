using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;

namespace RolixSAEProject.Services
{
    public class CustomerAccountService
    {
        private readonly ServiceClient _client;

        private const string AccountTable = "account";

        // Champs custom existants
        private const string ColIdentifiant = "crda6_identifiant";
        private const string ColMotDePasse = "crda6_motdepasse";

        // Email Dataverse standard demandé
        private const string ColEmail2 = "emailaddress2";

        public CustomerAccountService(ServiceClient client)
        {
            _client = client;
        }

        public AccountResetInfo? GetAccountByIdentifiant(string identifiant)
        {
            if (_client?.IsReady != true) return null;

            identifiant = (identifiant ?? "").Trim();
            if (identifiant.Length == 0) return null;

            var q = new QueryExpression(AccountTable)
            {
                ColumnSet = new ColumnSet("accountid", "name", ColIdentifiant, ColEmail2),
                Criteria =
        {
            Conditions =
            {
                new ConditionExpression(ColIdentifiant, ConditionOperator.Equal, identifiant),
            }
        },
                TopCount = 1
            };

            var res = _client.RetrieveMultiple(q);
            if (res.Entities.Count == 0) return null;

            var e = res.Entities[0];
            return new AccountResetInfo
            {
                AccountId = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? "",
                Identifiant = e.GetAttributeValue<string>(ColIdentifiant) ?? "",
                Email2 = e.GetAttributeValue<string>(ColEmail2) ?? ""
            };
        }



        // ✅ LOGIN EXISTANT
        public AccountProfile? ValidateLogin(string identifiant, string motDePasse)
        {
            if (_client?.IsReady != true) return null;
            identifiant = (identifiant ?? "").Trim();
            motDePasse = (motDePasse ?? "").Trim();

            if (identifiant.Length == 0 || motDePasse.Length == 0) return null;

            var q = new QueryExpression(AccountTable)
            {
                ColumnSet = new ColumnSet("accountid", "name", ColIdentifiant, ColMotDePasse),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ColIdentifiant, ConditionOperator.Equal, identifiant),
                        new ConditionExpression(ColMotDePasse, ConditionOperator.Equal, motDePasse),
                    }
                },
                TopCount = 1
            };

            var e = _client.RetrieveMultiple(q).Entities.Count > 0
                ? _client.RetrieveMultiple(q).Entities[0]
                : null;

            if (e == null) return null;

            return new AccountProfile
            {
                AccountId = e.Id.ToString(),
                NomCompte = e.GetAttributeValue<string>("name") ?? "",
                Identifiant = e.GetAttributeValue<string>(ColIdentifiant) ?? ""
            };
        }

        // ✅ PROFIL EXISTANT
        public AccountProfile? GetProfile(string accountId)
        {
            if (_client?.IsReady != true) return null;
            if (!Guid.TryParse(accountId, out var id)) return null;

            var e = _client.Retrieve(AccountTable, id, new ColumnSet("accountid", "name", ColIdentifiant));
            if (e == null) return null;

            return new AccountProfile
            {
                AccountId = e.Id.ToString(),
                NomCompte = e.GetAttributeValue<string>("name") ?? "",
                Identifiant = e.GetAttributeValue<string>(ColIdentifiant) ?? ""
            };
        }

        // =========================================================
        // ✅ AJOUTS POUR "MDP OUBLIÉ"
        // =========================================================

        public AccountResetInfo? GetAccountByEmail2(string email)
        {
            if (_client?.IsReady != true) return null;

            email = (email ?? "").Trim();
            if (email.Length == 0) return null;

            var q = new QueryExpression(AccountTable)
            {
                ColumnSet = new ColumnSet("accountid", "name", ColIdentifiant, ColEmail2),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ColEmail2, ConditionOperator.Equal, email),
                    }
                },
                TopCount = 1
            };

            var e = _client.RetrieveMultiple(q).Entities.Count > 0
                ? _client.RetrieveMultiple(q).Entities[0]
                : null;

            if (e == null) return null;

            return new AccountResetInfo
            {
                AccountId = e.Id,
                Name = e.GetAttributeValue<string>("name") ?? "",
                Identifiant = e.GetAttributeValue<string>(ColIdentifiant) ?? "",
                Email2 = e.GetAttributeValue<string>(ColEmail2) ?? ""
            };
        }

        public void UpdatePassword(Guid accountId, string newPassword)
        {
            if (_client?.IsReady != true) throw new InvalidOperationException("Dataverse client not ready");
            if (accountId == Guid.Empty) throw new Exception("Compte invalide.");
            newPassword = (newPassword ?? "").Trim();
            if (newPassword.Length < 4) throw new Exception("Mot de passe trop court.");

            var acc = new Microsoft.Xrm.Sdk.Entity(AccountTable, accountId);
            acc[ColMotDePasse] = newPassword;
            _client.Update(acc);
        }
    }
}
