using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Servicios.V1
{
    public class ServicioAutores : IServicioAutores
    {
        private readonly ApplicationDbContext context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IMapper mapper;

        public ServicioAutores(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.mapper = mapper;
        }

        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            //throw new NotImplementedException(); // Este es codigo de prueba para ver si funciona el middleware de manejo de excepciones.
            //var autores = await context.Autores.Include(x => x.Libros).ToListAsync();
            // AsQueryable es para convertir la consulta a una consulta que se pueda ejecutar en la base de datos, es decir,
            // se puede aplicar filtros, ordenamientos, paginacion, etc. y se ejecuta en la base de datos, lo que mejora el rendimiento.
            var queryable = context.Autores.AsQueryable();
            await httpContextAccessor.HttpContext!.InsertarParametroPaginacionEnCabecera(queryable);
            var autores = await queryable.OrderBy(x => x.Nombres).Paginar(paginacionDTO).ToListAsync();
            //var autores = await context.Autores.ToListAsync();
            //var autoresDTO = autores.Select(autor => 
            //                                    new AutorDTO 
            //                                    { 
            //                                        Id = autor.Id, 
            //                                        NombreCompleto = $"{autor.Nombres } {autor.Apellidos}" }
            //                                    );
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
            return autoresDTO;
            //return await context.Autores
            //    .Include(x => x.Libros)
            //    .ToListAsync();
        }
    }
}
