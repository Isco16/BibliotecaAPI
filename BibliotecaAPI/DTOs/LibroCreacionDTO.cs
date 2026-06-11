using BibliotecaAPI.Entidades;
using BibliotecaAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class LibroCreacionDTO
    {
        [Required]
        [StringLength(150, ErrorMessage = "El campo {0} debe tener 1 o menos de {1} caracteres")]
        [PrimeraLetraMayuscula]
        public required string Titulo { get; set; }
        public List<int> AutoresIds { get; set; } = []; // Llave Foranea
    }
}
