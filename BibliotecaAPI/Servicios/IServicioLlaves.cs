using BibliotecaAPI.Entidades;

namespace BibliotecaAPI.Servicios
{
    public interface IServicioLlaves
    {
        Task<LlaveAPI> CrearLlave(string usuarioId, TipoLLave tipoLlave);
        string GenerarLlave();
    }
}