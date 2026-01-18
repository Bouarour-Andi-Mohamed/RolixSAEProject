using System;
using System.Collections.Generic;

namespace RolixSAEProject.Models
{
    public class OrderSummary
    {
        public Guid OrderId { get; set; }
        public string Name { get; set; } = "";
        public DateTime? CreatedOn { get; set; }

        // statut simple (optionnel)
        public int? StatusCode { get; set; }
        public string StatusLabel { get; set; } = "";

        public List<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }

    public class OrderLine
    {
        public Guid OrderLineId { get; set; }
        public Guid? ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Quantity { get; set; }
    }
}
