// Models/Produit.cs
namespace RolixSAEProject.Models
{
    public class Produit
    {
        public int Id { get; set; }

        public string Nom { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;

        public decimal PrixEUR { get; set; }
        public decimal PrixCHF { get; set; }
        public decimal PrixUSD { get; set; }

        public string DescriptionFR { get; set; } = string.Empty;
        public string DescriptionEN { get; set; } = string.Empty;

        // Catégorie : Edition limitée / Collection Sport
        public string Categorie { get; set; } = string.Empty;

        // Collection : Classic / Pro
        public string Collection { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        // Genre : Femme / Homme / Unisex
        public string Genre { get; set; } = string.Empty;

        // --- NOUVEAUX CHAMPS (Dataverse) ---
        // Choice côté Dataverse, on stocke le "label" en string côté app
        public string Mouvement { get; set; } = string.Empty;   // Automatique / Manuel / Quartz / Autre
        public string Calibre { get; set; } = string.Empty;     // 3235
        public string Materiau { get; set; } = string.Empty;    // Acier / Or jaune / ...
        public string Bracelet { get; set; } = string.Empty;    // Oyster / Jubilee / ...
        public int TailleBoitierMm { get; set; }                // 41
        public string EtancheiteM { get; set; } = string.Empty;  // "200" (ou "200m" si tu préfères)
        public string Verre { get; set; } = string.Empty;       // Saphir / Minéral / ...
        public bool LoupeCyclope { get; set; }                  // true/false

        public decimal GetPrice(string currency)
        {
            return currency switch
            {
                "CHF" => PrixCHF,
                "USD" => PrixUSD,
                _ => PrixEUR
            };
        }
    }
}
