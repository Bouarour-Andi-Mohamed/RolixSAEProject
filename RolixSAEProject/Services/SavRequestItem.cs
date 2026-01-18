using System;

namespace RolixSAEProject.Models
{
    public class SavRequestItem
    {
        public Guid SavId { get; set; }
        public DateTime? CreatedOn { get; set; }

        public string? OrderName { get; set; }
        public Guid? OrderId { get; set; }

        public string? ProductName { get; set; }
        public Guid? ProductId { get; set; }

        public string DescriptionProbleme { get; set; } = "";
        public string StateLabel { get; set; } = "";   // Brouillon/Actif/etc. (si dispo)
        public string StatusLabel { get; set; } = "";  // Statut (si dispo)
    }
}