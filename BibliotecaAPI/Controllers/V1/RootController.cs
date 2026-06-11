using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.V1
{
    [ApiController]
    [Route("api/v1")]
    [Authorize]
    public class RootController: ControllerBase
    {
        private readonly IAuthorizationService authorizationService;

        public RootController(IAuthorizationService authorizationService)
        {
            this.authorizationService = authorizationService;
        }

        [HttpGet(Name = "ObtenerRootV1")]
        [AllowAnonymous]
        public async Task<IEnumerable<DatosHATEOASDTO>> Get()
        {
            var datosHATEOAS = new List<DatosHATEOASDTO>();

            var esAdmin = await authorizationService.AuthorizeAsync(User, "esAdmin");

            datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerRootV1", new { })!, Descripcion: "self", Metodo: "GET"));
            
            
            datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerAutoresV1", new { })!, Descripcion: "autores-obtener", Metodo: "GET"));

            datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerLibrosV1", new { })!, Descripcion: "libros-obtener", Metodo: "GET"));
            datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("RegistroUsuarioV1", new { })!, Descripcion: "usuario-registrar", Metodo: "POST"));
            datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("LoginUsuarioV1", new { })!, Descripcion: "usuario-login", Metodo: "POST"));
            
            if (User.Identity!.IsAuthenticated)
            {
                datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("ActualizarUsuarioV1", new { })!, Descripcion: "usuario-actualizar", Metodo: "PUT"));
                datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("RenovarTokenV1", new { })!, Descripcion: "token-renovar", Metodo: "GET"));
            }

            //Acciones que solo usuarios admin pueden realizar
            if (esAdmin.Succeeded)
            {
                datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("CrearAutorV1", new { })!, Descripcion: "autor-crear", Metodo: "POST"));
                datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("CrearAutoresV1", new { })!, Descripcion: "autores-crear", Metodo: "POST"));
                datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("CrearLibroV1", new { })!, Descripcion: "libro-crear", Metodo: "POST"));
                datosHATEOAS.Add(new DatosHATEOASDTO(Enlace: Url.Link("ObtenerUsuariosV1", new { })!, Descripcion: "usuarios-obtener", Metodo: "GET"));

            }

            return datosHATEOAS;
        }
    }
}
