using System.ComponentModel.DataAnnotations;

namespace RolixSAEProject.Models
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Code requis.")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Nouveau mot de passe requis.")]
        [MinLength(4, ErrorMessage = "Minimum 4 caractères.")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Confirmation requise.")]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string ConfirmPassword { get; set; } = "";

        public string? Error { get; set; }
        public string? Info { get; set; }
    }
}
