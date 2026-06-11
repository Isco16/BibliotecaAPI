using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Linq.Dynamic.Core; // Para poder usar el OrderBy con string, se instala el paquete System.Linq.Dynamic.Core desde NuGet

namespace BibliotecaAPI.Controllers.V2
{
    [ApiController] // Ayuda a configurar como controladores para Web APIs
    [Route("api/v2/autores")] // Define la ruta o endpoint donde apunta este controlador
    [Authorize(Policy = "esAdmin")]
    public class AutoresController : ControllerBase // Conjunto elementos auxiliares para desarrollar WebAPIs
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        // Inyección de dependencias para el servicio de logging, que permite registrar información sobre la
        // ejecución de la aplicación, como errores, advertencias, información general, etc., lo que es útil
        // para el monitoreo y la depuración de la aplicación.
        private readonly ILogger<AutoresController> logger;
        // Inyección de dependencias para el servicio de almacenamiento en caché de salida, que permite almacenar
        // en caché las respuestas de los endpoints para mejorar el rendimiento y reducir la carga en el servidor,
        // lo que es especialmente útil para endpoints que se consultan con frecuencia y no cambian con frecuencia.
        private readonly IOutputCacheStore outputCacheStore;
        private readonly IServicioAutores servicioAutoresV1;
        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        public AutoresController(ApplicationDbContext context,
            IMapper mapper, 
            IAlmacenadorArchivos almacenadorArchivos, 
            ILogger<AutoresController> logger,
            IOutputCacheStore outputCacheStore,
            IServicioAutores servicioAutoresV1
            )
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.logger = logger;
            this.outputCacheStore = outputCacheStore;
            this.servicioAutoresV1 = servicioAutoresV1;
        }

        [HttpGet] // Se pueden tener multiples rutas para un mismo endpoint
        [AllowAnonymous] // Permite el acceso a este endpoint sin necesidad de autenticación, incluso si el controlador tiene la anotación [Authorize], esta anotación se sobreescribe para este endpoint en particular, lo que permite que cualquier usuario, autenticado o no, pueda acceder a este endpoint específico.
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            return await servicioAutoresV1.Get(paginacionDTO);
        }

        [HttpGet("{id:int}", Name ="ObtenerAutorV2")] // api/autores/id
        [AllowAnonymous]
        [EndpointSummary("Obtiene autor por Id")]
        [EndpointDescription("Obtiene a un autor por su Id. Incluye sus libros. Si el autor no existe,s e retorna 404.")]
        [ProducesResponseType<AutorConLibrosDTO>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<AutorConLibrosDTO>> Get([FromRoute][Description("El id del autor")]int id, bool incluirLibros = false) // FromQuery es para resivir explicitamente QueryStrings
        {
            var queryable = context.Autores.AsQueryable();

            if (incluirLibros)
            {
                queryable = queryable
                    .Include(x => x.Libros)
                    .ThenInclude(x => x.Libro);
            }

            var autor = await queryable.FirstOrDefaultAsync(x => x.Id == id);

            if(autor is null)
            {
                return NotFound();
            }

            var autorDTO = mapper.Map<AutorConLibrosDTO>(autor);

            return autorDTO;
        }

        [HttpGet("filtrar")]
        [AllowAnonymous]
        public async Task<ActionResult> Filtrar([FromQuery] AutorFiltroDTO autorFiltroDTO)
        {
            // Ejecucion diferida, se construye la consulta pero no se ejecuta hasta que se llama a un metodo
            // que ejecuta la consulta, como ToListAsync, FirstOrDefaultAsync, etc.
            var queryable = context.Autores.AsQueryable();

            if (!string.IsNullOrEmpty(autorFiltroDTO.Nombres))
            {
                queryable = queryable.Where(x => x.Nombres.Contains(autorFiltroDTO.Nombres));
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.Apellidos))
            {
                queryable = queryable.Where(x => x.Apellidos.Contains(autorFiltroDTO.Apellidos));
            }

            if (autorFiltroDTO.IncluirLibros)
            {
                queryable = queryable.Include(x => x.Libros).ThenInclude(x => x.Libro);
            }

            if (autorFiltroDTO.TieneFoto.HasValue)
            {
                if (autorFiltroDTO.TieneFoto.Value)
                {
                    queryable = queryable.Where(x => x.Foto != null);
                }
                else
                {
                    queryable = queryable.Where(x => x.Foto == null);
                }
            }

            if (autorFiltroDTO.TieneLibros.HasValue)
            {
                if (autorFiltroDTO.TieneLibros.Value)
                {
                    queryable = queryable.Where(x => x.Libros.Any());
                }
                else
                {
                    queryable = queryable.Where(x => !x.Libros.Any());
                }
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.TituloLibro))
            {
                queryable = queryable.Where(
                    x => x.Libros.Any(
                        y => y.Libro!.Titulo.Contains(autorFiltroDTO.TituloLibro!))
                    )
                ;
            }

            if (!string.IsNullOrEmpty(autorFiltroDTO.CampoOrdenar))
            {
                var tipoOrden = autorFiltroDTO.OrdenAscendente ? "ascending" : "descending";

                try
                {
                    queryable = queryable.OrderBy($"{autorFiltroDTO.CampoOrdenar} {tipoOrden}");
                }
                catch(Exception ex)
                {
                    queryable = queryable.OrderBy(x => x.Nombres);
                    logger.LogError(ex.Message, ex);
                    //logger.LogError(ex, "Error al ordenar por el campo {CampoOrdenar}", autorFiltroDTO.CampoOrdenar);
                }
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Nombres);
            }

            var autores = await queryable
                .Paginar(autorFiltroDTO.PaginacionDTO)
                .ToListAsync();

            if (autorFiltroDTO.IncluirLibros)
            {
                var autoresLibrosDTO = mapper.Map<IEnumerable<AutorConLibrosDTO>>(autores);
                return Ok(autoresLibrosDTO);
            }
            else
            {
                var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
                return Ok(autoresDTO);
            }


        }

        [HttpPost]
        public async Task<ActionResult>Post([FromBody] AutorCreacionDTO autorCreacionDTO) // Fuentes de variables desde el Body o Header de un Http Request
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);
            context.Add(autor);
            //context.Autores.Add(autor);// Otra forma de agregar
            await context.SaveChangesAsync(); // Guarda los cambios en la base de datos asincronico
            // EvictByTagAsync es para eliminar la cache de salida por su tag, en este caso el tag es "autores-obtener",
            // lo que hace que la cache se elimine cada vez que se agrega un nuevo autor, lo que garantiza que la cache
            // siempre tenga los datos actualizados.
            await outputCacheStore.EvictByTagAsync(cache, default); 

            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDTO);
        }

        [HttpPost("con-foto")]
        public async Task<ActionResult> PostConFoto([FromForm] AutorCreacionDTOConFoto autorCreacionDTO) // Fuentes de variables desde el Body o Header de un Http Request
        {
            var autor = mapper.Map<Autor>(autorCreacionDTO);

            if(autorCreacionDTO.Foto is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            context.Add(autor);
            //context.Autores.Add(autor);// Otra forma de agregar
            await context.SaveChangesAsync(); // Guarda los cambios en la base de datos asincronico
            //return Ok();
            await outputCacheStore.EvictByTagAsync(cache, default);

            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutorV2", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}")] //api/autores/id
        public async Task<ActionResult>Put(int id, [FromForm] AutorCreacionDTOConFoto autorCreacionDTO)
        {
            var existeAutor = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existeAutor)
            {
                return NotFound();
            }

            var autor = mapper.Map<Autor>(autorCreacionDTO);
            //if(id != autor.Id)
            //{
            //    return BadRequest("los dis deben de coincidir");
            //}

            autor.Id = id;

            if(autorCreacionDTO.Foto is not null)
            {
                var fotoActual = await context.Autores.Where(x => x.Id == id).Select(x => x.Foto).FirstAsync();
                var url = await almacenadorArchivos.Editar(fotoActual, contenedor, autorCreacionDTO.Foto);
                autor.Foto = url;
            }

            context.Update(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            //return Ok();
            return NoContent(); // Es una buena practica retornar NoContent en los Put, ya que no se retorna nada, y es mas eficiente que retornar Ok con un objeto vacio.
        }

        [HttpPatch("{id:int}")]
        public async Task<ActionResult> Patch(int id, JsonPatchDocument<AutorPatchDTO> patchDocument)
        {
            if (patchDocument is null)
            {
                return BadRequest();
            }

            var autor = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);
            
            if (autor is null)
            {
                return NotFound();
            }

            var autorPatchDTO = mapper.Map<AutorPatchDTO>(autor);

            patchDocument.ApplyTo(autorPatchDTO, ModelState); // Aplica los cambios del patchDocument al autorPatchDTO, y el ModelState es para validar los cambios aplicados, si hay errores de validacion se agregan al ModelState

            var esValido = TryValidateModel(autorPatchDTO); // Valida el autorPatchDTO despues de aplicar los cambios del patchDocument, si hay errores de validacion se agregan al ModelState

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(autorPatchDTO, autor); // Mapea el autorPatchDTO al autor, es decir, actualiza el autor con los cambios del autorPatchDTO

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var autor = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if(autor is null)
            {
                return NotFound();
            }

            context.Remove(autor);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            await almacenadorArchivos.Borrar(autor.Foto, contenedor);

            //var registrosBorrados = await context.Autores.Where(x => x.Id == id).ExecuteDeleteAsync();

            //if(registrosBorrados == 0)
            //{
            //    return NotFound();
            //}

            //return Ok();
            return NoContent(); // Es una buena practica retornar NoContent en los Put, ya que no se retorna nada, y es mas eficiente que retornar Ok con un objeto vacio.
        }
    }
}
