using System;

namespace RolixSAEProject.Models
{
    public class InvoiceSummary
    {
        public Guid InvoiceId { get; set; }

        public string Name { get; set; } = "";
        public DateTime? CreatedOn { get; set; }

        public decimal? TotalAmount { get; set; }

        public string? OrderName { get; set; }

        public int? StatusCode { get; set; }
        public string StatusLabel { get; set; } = "";
    }
}
