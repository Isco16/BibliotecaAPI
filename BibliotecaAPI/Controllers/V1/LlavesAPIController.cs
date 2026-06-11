using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers.V1
{
    [Route("api/v1/llavesapi")]
    [Authorize]
    [ApiController]
    [DeshabilitarLimitarPeticiones] // No es necesario colocar el texto de Attribute debido a que el editor sabe que es un atributo
    public class LlavesAPIController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServicioLlaves servicioLlaves;
        private readonly IServiciosUsuarios serviciosUsuarios;

        public LlavesAPIController(ApplicationDbContext context, IMapper mapper, IServicioLlaves servicioLlaves, IServiciosUsuarios serviciosUsuarios)
        {
            this.context = context;
            this.mapper = mapper;
            this.servicioLlaves = servicioLlaves;
            this.serviciosUsuarios = serviciosUsuarios;
        }

        [HttpGet]
        public async Task<IEnumerable<LlaveDTO>> Get()
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var llaves = await context.LlavesAPI
                .Include(x => x.RestriccionesDominio)
                .Include(x => x.RestriccionesIP)
                .Where(x => x.UsuarioId == usuarioId).ToListAsync();
            return mapper.Map<IEnumerable<LlaveDTO>>(llaves);
        }

        [HttpGet("{id:int}", Name = "ObtenerLlavesV1")]
        public async Task<ActionResult<LlaveDTO>> Get(int id)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var llaves = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == id);

            if (llaves is null)
            {
                return NotFound(); // 404
            }

            if (llaves.UsuarioId != usuarioId)
            {
                return Forbid(); // 403
            }

            return mapper.Map<LlaveDTO>(llaves);
        }

        [HttpPost]
        public async Task<ActionResult> Post(LlaveCreacionDTO llaveCreacionDTO)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId()!;

            if (llaveCreacionDTO.TipoLlave == TipoLLave.Gratuita)
            {
                var elUsuarioYaTieneLlaveGratuita = await context.LlavesAPI.AnyAsync(x => x.UsuarioId == usuarioId && x.TipoLLave == llaveCreacionDTO.TipoLlave);

                if (elUsuarioYaTieneLlaveGratuita)
                {
                    ModelState.AddModelError(nameof(llaveCreacionDTO.TipoLlave), "El usuario ya tiene una llave gratuita");
                    return ValidationProblem();
                }
            }

            var llaveAPI = await servicioLlaves.CrearLlave(usuarioId, llaveCreacionDTO.TipoLlave);
            var llaveDTO = mapper.Map<LlaveDTO>(llaveAPI);
            return CreatedAtRoute("ObtenerLlavesV1", new { id = llaveAPI.Id}, llaveDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, LlaveActualizacionDTO llaveActualizacionDTO)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var llaveDB = await context.LlavesAPI.FirstOrDefaultAsync(x => x.Id == id);

            if(llaveDB is null)
            {
                return NotFound(); // 404
            }

            if(usuarioId != llaveDB.UsuarioId)
            {
                return Forbid(); // 403
            }

            // Mecanismo para actualizar llaves de usuarios en caso de ser necesario
            if (llaveActualizacionDTO.ActualizarLlave)
            {
                llaveDB.Llave = servicioLlaves.GenerarLlave();
            }

            llaveDB.Activa = llaveActualizacionDTO.Activa;
            await context.SaveChangesAsync();
            return NoContent(); // 204
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var llaveDB = await context.LlavesAPI.FirstOrDefaultAsync(x =>x.Id == id);

            if(llaveDB is null)
            {
                return NotFound();
            }

            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            if(usuarioId != llaveDB.UsuarioId)
            {
                return Forbid();
            }

            if(llaveDB.TipoLLave == TipoLLave.Gratuita)
            {
                ModelState.AddModelError("", "No puedes borrar una llave gratiuta");
                return ValidationProblem();
            }

            context.LlavesAPI.Remove(llaveDB);
            await context.SaveChangesAsync();
            return NoContent(); // 204
        }
    }
}