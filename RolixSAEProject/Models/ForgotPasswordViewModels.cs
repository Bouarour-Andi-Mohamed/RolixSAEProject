using System.ComponentModel.DataAnnotations;

namespace RolixSAEProject.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Identifiant requis.")]
        public string Identifiant { get; set; } = "";

        public string? Info { get; set; }
        public string? Error { get; set; }
    }
}
