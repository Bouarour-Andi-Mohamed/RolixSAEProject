using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RolixSAEProject.Models
{
    public class SavCreateViewModel
    {
        [Required]
        public Guid OrderId { get; set; }

        // produit choisi dans la commande
        [Required(ErrorMessage = "Choisis le produit concerné.")]
        public Guid ProductId { get; set; }

        [Required(ErrorMessage = "Décris le problème.")]
        [StringLength(2000, ErrorMessage = "Maximum 2000 caractères.")]
        public string ProblemDescription { get; set; } = "";

        // pour l’affichage
        public List<(Guid ProductId, string ProductName)> Products { get; set; }
            = new List<(Guid, string)>();
    }
}
