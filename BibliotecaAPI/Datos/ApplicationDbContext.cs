using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Datos
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Autor>().Property(x => x.Nombres).HasMaxLength(150);
            // Configura un filtro global para la entidad Comentario, de manera que solo se incluyan en las
            // consultas los comentarios que no estén marcados como borrados (EstaBorrado = false).
            // Esto es útil para implementar un borrado lógico, donde los comentarios no se eliminan físicamente
            // de la base de datos, sino que se marcan como borrados para mantener el historial de comentarios y
            // evitar problemas de integridad referencial con otros registros que puedan estar relacionados con
            // el comentario, como por ejemplo, respuestas a ese comentario o votos a ese comentario.
            modelBuilder.Entity<Comentario>().HasQueryFilter(b => !b.EstaBorrado);
        }

        public DbSet<Autor> Autores { get; set; }
        public DbSet<Libro> Libros { get; set; }

        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<AutorLibro> AutoresLibros { get; set; }
        public DbSet<Error> Errores {  get; set; }

        public DbSet<LlaveAPI> LlavesAPI { get; set; }
        public DbSet<Peticion> Peticiones { get; set; }
        public DbSet<RestriccionDominio> RestriccionesDominio { get; set; }
        public DbSet<RestriccionIP> RestriccionesIP { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<FacturaEmitida> FacturasEmitidas { get; set; }
    }
}
