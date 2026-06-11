namespace BibliotecaAPI.DTOs
{
    public class AutorConLibrosDTO: AutorDTO
    {
        public List<LibroDTO> Libros { get; set; } = []; // El [] es equivalente a new List<LibroDTO>()
    }
}
