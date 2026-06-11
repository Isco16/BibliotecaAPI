using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entidades
{
    [PrimaryKey("Mes","Año")] // Llave primaria compuesta
    public class FacturaEmitida
    {
        public int Mes { get; set; }
        public int Año { get; set; }
    }
}
