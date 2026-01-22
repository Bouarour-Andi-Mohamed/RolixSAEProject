using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Collections.Generic;

namespace RolixSAEProject.Services
{
    public class InvoiceService
    {
        private readonly ServiceClient _client;

        public InvoiceService(ServiceClient client)
        {
            _client = client;
        }

        public List<InvoiceSummary> GetInvoicesForAccount(Guid accountId, int top = 50)
        {
            var list = new List<InvoiceSummary>();

            var q = new QueryExpression("invoice")
            {
                ColumnSet = new ColumnSet(
                    "invoiceid",
                    "name",
                    "createdon",
                    "totalamount",
                    "salesorderid",
                    "statuscode",
                    "customerid"
                ),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("customerid", ConditionOperator.Equal, accountId)
                    }
                },
                TopCount = top
            };

            q.AddOrder("createdon", OrderType.Descending);

            var rows = _client.RetrieveMultiple(q).Entities;

            foreach (var e in rows)
            {
                e.FormattedValues.TryGetValue("statuscode", out var statusLabel);

                var orderRef = e.GetAttributeValue<EntityReference>("salesorderid");
                var total = e.GetAttributeValue<Money>("totalamount");

                list.Add(new InvoiceSummary
                {
                    InvoiceId = e.Id,
                    Name = e.GetAttributeValue<string>("name") ?? "",
                    CreatedOn = e.GetAttributeValue<DateTime?>("createdon"),
                    TotalAmount = total?.Value,

                    OrderName = orderRef?.Name,

                    StatusCode = e.GetAttributeValue<OptionSetValue>("statuscode")?.Value,
                    StatusLabel = statusLabel ?? ""
                });
            }

            return list;
        }
    }
}
