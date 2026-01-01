using System;
using System.ComponentModel.DataAnnotations;

namespace RolixSAEProject.Models
{
    public class QuoteRequestViewModel
    {
        // Produit (référence page)
        public int ProductLocalId { get; set; }
        public Guid ProductDataverseId { get; set; }

        // Devise du site (cookie)
        public string Currency { get; set; } = "EUR";

        // Affichage produit
        public Produit? Product { get; set; }

        // Formulaire
        [Range(1, 999, ErrorMessage = "Quantité invalide")]
        public decimal Quantity { get; set; } = 1;

        [Required(ErrorMessage = "Merci d’indiquer la raison de votre demande.")]
        [StringLength(1000, ErrorMessage = "Maximum 1000 caractères.")]
        public string Reason { get; set; } = "";

        [StringLength(200, ErrorMessage = "Maximum 200 caractères.")]
        public string Usage { get; set; } = ""; // ex: cadeau, usage pro, événement...

        [StringLength(200, ErrorMessage = "Maximum 200 caractères.")]
        public string ContactPreference { get; set; } = "Email"; // Email / Téléphone

        [EmailAddress(ErrorMessage = "Email invalide")]
        [StringLength(120)]
        public string? ContactEmail { get; set; }

        [StringLength(40)]
        public string? ContactPhone { get; set; }

        [StringLength(1000, ErrorMessage = "Maximum 1000 caractères.")]
        public string? Notes { get; set; }
    }
}
