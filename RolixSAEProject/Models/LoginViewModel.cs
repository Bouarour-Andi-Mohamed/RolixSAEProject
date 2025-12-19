using System.ComponentModel.DataAnnotations;

namespace RolixSAEProject.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Identifiant requis")]
        [Display(Name = "Identifiant")]
        public string Identifiant { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mot de passe requis")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string MotDePasse { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
