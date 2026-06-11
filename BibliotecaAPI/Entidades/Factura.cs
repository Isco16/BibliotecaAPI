using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entidades
{
    public class Factura
    {
        public int Id { get; set; }
        public required string UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public bool Pagada { get; set; }
        [Precision(18,2)] // Decimal de 18 digitos y de las cuales 2 son decimales
        public decimal Monto { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaLimiteDePago { get; set; }
    }
}
