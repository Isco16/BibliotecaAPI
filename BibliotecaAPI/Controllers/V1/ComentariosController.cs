using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1/libros/{libroId:int}/comentarios")]
    [Authorize]
    public class ComentariosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServiciosUsuarios servicioUsuarios;
        private readonly IOutputCacheStore outputCacheStore;
        private const string cache = "comentarios-obetner";

        public ComentariosController(ApplicationDbContext context, IMapper mapper, IServiciosUsuarios servicioUsuarios, IOutputCacheStore outputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.servicioUsuarios = servicioUsuarios;
            this.outputCacheStore = outputCacheStore;
        }

        [HttpGet(Name = "ObtenerComentariosV1")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<IEnumerable<ComentarioDTO>>> Get(int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync(libro => libro.Id == libroId);

            if (!existeLibro)
            {
                return NotFound();
            }

            var comentarios = await context.Comentarios
                .Include(x => x.Usuario)
                .Where(comentario => comentario.LibroId == libroId)
                .OrderByDescending(comentario => comentario.FechaPublicacion)
                .ToListAsync();
            var comentariosDTO = mapper.Map<List<ComentarioDTO>>(comentarios);
            return comentariosDTO;
        }

        [HttpGet("{id}", Name = "ObtenerComentarioV1")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<ComentarioDTO>> Get(Guid id)
        {
            var comentario = await context.Comentarios
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(comentario => comentario.Id == id);
            if (comentario is null)
            {
                return NotFound();
            }
            var comentarioDTO = mapper.Map<ComentarioDTO>(comentario);
            return comentarioDTO;
        }

        [HttpPost(Name = "CrearComentarioV1")]
        public async Task<ActionResult> Post(int libroId, ComentarioCreacionDTO comentarioCreacionDTO)
        {
            var existeLibro = await context.Libros.AnyAsync(libro => libro.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }
            var usuario = await servicioUsuarios.ObtenerUsuario();
            if(usuario is null)
            {
                return NotFound("No se encontro el usuario");
            }
            var comentario = mapper.Map<Comentario>(comentarioCreacionDTO);
            comentario.LibroId = libroId;
            comentario.FechaPublicacion = DateTime.UtcNow;
            comentario.UsuarioId = usuario.Id;
            context.Add(comentario);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default); // Eliminar la cache de los comentarios, para que se actualice la cache con el nuevo comentario creado.
            var comentarioDTO = mapper.Map<ComentarioDTO>(comentario);
            return CreatedAtRoute("ObtenerComentarioV1", new { id = comentario.Id, libroId = libroId }, comentarioDTO);
        }

        [HttpPatch("{id}", Name = "PatchComentarioV1")]
        public async Task<ActionResult> Patch(Guid id, int libroId, JsonPatchDocument<ComentarioPatchDTO> patchDocument)
        {
            if (patchDocument is null)
            {
                return BadRequest();
            }

            var existeLibro = await context.Libros.AnyAsync(libro => libro.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await servicioUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return NotFound("No se encontro el usuario");
            }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
            {
                return NotFound();
            }

            if(comentarioDB.UsuarioId != usuario.Id)
            {
                return Forbid();
            }

            var comentarioPatchDTO = mapper.Map<ComentarioPatchDTO>(comentarioDB);

            patchDocument.ApplyTo(comentarioPatchDTO, ModelState); // Aplica los cambios del patchDocument al autorPatchDTO, y el ModelState es para validar los cambios aplicados, si hay errores de validacion se agregan al ModelState

            var esValido = TryValidateModel(comentarioPatchDTO); // Valida el autorPatchDTO despues de aplicar los cambios del patchDocument, si hay errores de validacion se agregan al ModelState

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(comentarioPatchDTO, comentarioDB); // Mapea el autorPatchDTO al autor, es decir, actualiza el autor con los cambios del autorPatchDTO

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default); // Eliminar la cache de los comentarios, para que se actualice la cache con el nuevo comentario creado.

            return NoContent();
        }

        [HttpDelete("{id}", Name = "BorrarComentarioV1")]
        public async Task<ActionResult> Delete(Guid id, int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync(libro => libro.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await servicioUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return NotFound("No se encontro el usuario");
            }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if(comentarioDB is null)
            {
                return NotFound();
            }

            if(comentarioDB.UsuarioId != usuario.Id)
            {
                return Forbid();
            }

            comentarioDB.EstaBorrado = true; // En lugar de eliminar el comentario de la base de datos, se marca como borrado lógico, para mantener el historial de comentarios y evitar problemas de integridad referencial con otros registros que puedan estar relacionados con el comentario, como por ejemplo, respuestas a ese comentario o votos a ese comentario.
            //context.Remove(comentarioDB);
            context.Update(comentarioDB);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default); // Eliminar la cache de los comentarios, para que se actualice la cache con el nuevo comentario creado.

            return NoContent();
        }
    }
}
