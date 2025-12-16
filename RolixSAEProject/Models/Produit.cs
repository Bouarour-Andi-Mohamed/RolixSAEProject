// Models/Produit.cs
namespace RolixSAEProject.Models
{
    public class Produit
    {
        public int Id { get; set; }

        public string Nom { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;

        public decimal PrixEUR { get; set; }

        public string DescriptionFR { get; set; } = string.Empty;
        public string DescriptionEN { get; set; } = string.Empty;

        // Catégorie : Edition limitée / Collection Sport
        public string Categorie { get; set; } = string.Empty;

        // Collection : Classic / Pro
        public string Collection { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        // Genre : Femme / Homme / Unisex
        public string Genre { get; set; } = string.Empty;
    }
}
