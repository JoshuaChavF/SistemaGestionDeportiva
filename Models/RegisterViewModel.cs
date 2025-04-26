using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionDeportiva.Models
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        [Display(Name = "Tipo de Usuario")]
        public string TipoUsuario { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la contraseña de confirmación no coinciden.")]
        public string ConfirmPassword { get; set; }
    }
}