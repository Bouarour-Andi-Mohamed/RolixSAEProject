using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class OrderService
    {
        private readonly ServiceClient _client;

        // ✅ Change ça si ta table "Commandes" a un autre nom logique
        private const string OrderTable = "salesorder";
        private const string OrderLineTable = "salesorderdetail";

        public OrderService(ServiceClient client)
        {
            _client = client;
        }

        public List<OrderSummary> GetOrdersForAccount(Guid accountId, int top = 20)
        {
            if (_client?.IsReady != true) return new List<OrderSummary>();

            // 1) commandes
            var orderQuery = new QueryExpression(OrderTable)
            {
                ColumnSet = new ColumnSet("salesorderid", "name", "createdon", "statuscode"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("customerid", ConditionOperator.Equal, accountId)
                    }
                },
                Orders =
                {
                    new OrderExpression("createdon", OrderType.Descending)
                },
                TopCount = top
            };

            var orders = _client.RetrieveMultiple(orderQuery).Entities;

            // 2) map commandes
            var result = orders.Select(o =>
            {
                o.FormattedValues.TryGetValue("statuscode", out var statusLabel);

                return new OrderSummary
                {
                    OrderId = o.Id,
                    Name = o.GetAttributeValue<string>("name") ?? "",
                    CreatedOn = o.GetAttributeValue<DateTime?>("createdon"),
                    StatusCode = o.GetAttributeValue<OptionSetValue>("statuscode")?.Value,
                    StatusLabel = statusLabel ?? ""
                };
            }).ToList();

            if (result.Count == 0) return result;

            // 3) lignes commande pour toutes les commandes d'un coup
            var orderIds = result.Select(x => x.OrderId).ToArray();

            var lineQuery = new QueryExpression(OrderLineTable)
            {
                ColumnSet = new ColumnSet("salesorderdetailid", "salesorderid", "productid", "quantity"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("salesorderid", ConditionOperator.In, orderIds.Cast<object>().ToArray())
                    }
                }
            };

            var lines = _client.RetrieveMultiple(lineQuery).Entities;

            // group by commande
            var byOrder = lines
                .GroupBy(l => l.GetAttributeValue<EntityReference>("salesorderid")?.Id ?? Guid.Empty)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var o in result)
            {
                if (!byOrder.TryGetValue(o.OrderId, out var orderLines)) continue;

                o.Lines = orderLines.Select(l =>
                {
                    var prodRef = l.GetAttributeValue<EntityReference>("productid");

                    return new OrderLine
                    {
                        OrderLineId = l.Id,
                        ProductId = prodRef?.Id,
                        ProductName = prodRef?.Name ?? "",
                        Quantity = (decimal)(l.GetAttributeValue<decimal?>("quantity") ?? 0m)
                    };
                }).ToList();
            }

            return result;
        }

        public List<(Guid ProductId, string ProductName)> GetProductsForOrder(Guid orderId)
        {
            if (_client?.IsReady != true) return new List<(Guid, string)>();

            var q = new QueryExpression(OrderLineTable)
            {
                ColumnSet = new ColumnSet("productid", "salesorderid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression("salesorderid", ConditionOperator.Equal, orderId)
                    }
                }
            };

            var lines = _client.RetrieveMultiple(q).Entities;

            // Produits distincts
            var products = lines
                .Select(l => l.GetAttributeValue<EntityReference>("productid"))
                .Where(p => p != null && p.Id != Guid.Empty)
                .GroupBy(p => p!.Id)
                .Select(g => (ProductId: g.Key, ProductName: g.First()!.Name ?? "Produit"))
                .ToList();

            return products;
        }
    }
}
