using BibliotecaAPI.DTOs;

namespace BibliotecaAPI.Utilidades
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> Paginar<T>(this IQueryable<T> queryable, PaginacionDTO paginacionDTO)
        {
            return queryable
                .Skip((paginacionDTO.Pagina - 1) * paginacionDTO.RecordsPorPagina) // Skip para saltar los registros anteriores a la página actual
                .Take(paginacionDTO.RecordsPorPagina); // Take para tomar solo la cantidad de registros por página
        }
    }
}
