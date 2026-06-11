using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.Entidades
{
    public class Comentario
    {
        public Guid Id { get; set; }
        [Required]
        public required string Cuerpo { get; set; }
        public DateTime FechaPublicacion { get; set; }
        public int LibroId { get; set; } // Por convencion del Framework para hacer relacion es de uno a muchos se crea un campo con el nombre de la entidad fuerte seguido de la palabra "Id" para hacer el nexo del registro
        public Libro? Libro { get; set; } // Este campo indica que la entiodad Comentario es de relacion de muchos comentarios a un libro.
        public required string UsuarioId { get; set; }
        public bool EstaBorrado { get; set; }
        public Usuario? Usuario { get; set; }
    }
}
