using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;

namespace RolixSAEProject.Services
{
    public class CustomerAuthService
    {
        private readonly ServiceClient _client;

        public CustomerAuthService(ServiceClient client)
        {
            _client = client;
        }

        public string? ValidateLogin(string identifiant, string motDePasse)
        {
            if (_client?.IsReady != true) return null;

            var q = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("accountid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("crda6_identifiant", ConditionOperator.Equal, identifiant),
                        new ConditionExpression("crda6_motdepasse", ConditionOperator.Equal, motDePasse),
                    }
                },
                TopCount = 1
            };

            var e = _client.RetrieveMultiple(q).Entities.FirstOrDefault();
            return e?.Id.ToString();
        }
    }
}
