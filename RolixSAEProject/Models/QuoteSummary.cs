using System;

namespace RolixSAEProject.Models
{
    public class QuoteSummary
    {
        public Guid QuoteId { get; set; }

        public string Name { get; set; } = "";
        public DateTime? CreatedOn { get; set; }

        public string? CurrencyName { get; set; }      // ex: euro / franc suisse / US Dollar
        public string? PriceListName { get; set; }     // ex: Tarifications en EUR / Tarification en CHF / Tarification US

        public decimal? TotalAmount { get; set; }

        public string StateLabel { get; set; } = "";   // ex: Brouillon
        public string StatusLabel { get; set; } = "";  // ex: Actif / etc.
    }
}
