using System;

namespace RolixSAEProject.Models
{
    public class SavRequestItem
    {
        public Guid SavId { get; set; }
        public DateTime? CreatedOn { get; set; }

        public Guid? OrderId { get; set; }
        public string? OrderName { get; set; }

        public Guid? ProductId { get; set; }
        public string? ProductName { get; set; }

        public string DescriptionProbleme { get; set; } = "";

        // labels Dataverse
        public string StateLabel { get; set; } = "";
        public string StatusLabel { get; set; } = "";

        // ✅ Choice crda6_statusav
        public int? StatusSavValue { get; set; }        // 0/1/2
        public string StatusSavLabel { get; set; } = ""; // "Accepté" etc.
    }
}
