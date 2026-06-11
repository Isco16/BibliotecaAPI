using BibliotecaAPI.Datos;
using BibliotecaAPI.Entidades;

namespace BibliotecaAPI.Servicios
{
    public class ServicioLlaves : IServicioLlaves
    {
        private readonly ApplicationDbContext context;

        public ServicioLlaves(ApplicationDbContext context)
        {
            this.context = context;
        }

        // Metodo para insertar una llave a la base de datos
        public async Task<LlaveAPI> CrearLlave(string usuarioId, TipoLLave tipoLlave)
        {
            var llave = GenerarLlave();

            var llaveAPI = new LlaveAPI
            {
                Activa = true,
                Llave = llave,
                TipoLLave = tipoLlave,
                UsuarioId = usuarioId,
            };

            // Agrega un registro en memoria
            context.Add(llaveAPI);

            // Se aplican los cambios a la base de datos
            await context.SaveChangesAsync();

            return llaveAPI;
        }

        public string GenerarLlave() => Guid.NewGuid().ToString().Replace("-", "");// Se le quitan los guiones
    }
}
