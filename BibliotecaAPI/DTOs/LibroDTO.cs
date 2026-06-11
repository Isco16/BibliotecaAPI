using BibliotecaAPI.Entidades;
using BibliotecaAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class LibroDTO
    {
        public int Id { get; set; }
        public required string Titulo { get; set; }
    }
}
