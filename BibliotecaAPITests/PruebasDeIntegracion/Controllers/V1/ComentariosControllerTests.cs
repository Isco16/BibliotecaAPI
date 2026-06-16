using BibliotecaAPI.Entidades;
using BibliotecaAPITests.Utilidades;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace BibliotecaAPITests.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class ComentariosControllerTests : BasePruebas
    {
        private readonly string url = "/api/v1/libros/1/comentarios";
        private string nombreBD = Guid.NewGuid().ToString();

        private async Task CrearDataDePrueba()
        {
            var context = ConstruirContext(nombreBD);
            var autor = new Autor { Nombres = "Felipe", Apellidos = "Gavilan" };
            context.Add(autor);
            await context.SaveChangesAsync();

            var libro = new Libro { Titulo = "titulo"};
            libro.Autores.Add(new AutorLibro { Autor = autor});
            context.Add(libro);
            await context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task Delete_Devuleve204_CuadnoUsuarioBorraSuPropioComentario()
        {
            // Preparacion
            await CrearDataDePrueba();
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);


            var claims = new List<Claim> { adminClaim };

            string email = "ejemplo@hotmail.com";

            var token = await CrearUsuario(nombreBD, factory, claims, email);
            string llaveAPI = await ObtenerAPIKey(nombreBD, factory, email, TipoLLave.Profesional); // Se obtiene la llave API

            // Ahora se le envia la lista de claims
            var context = ConstruirContext(nombreBD);
            var usuario = await context.Users.FirstAsync();// el unico usuario que debe existir

            var comentario = new Comentario
            {
                Cuerpo = "contenido",
                UsuarioId = usuario!.Id,
                LibroId = 1
            };

            context.Comentarios.Add(comentario);

            await context.SaveChangesAsync();

            var cliente = factory.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            cliente.DefaultRequestHeaders.Add("X-Api-Key", llaveAPI); // Se agrega la llave API en el header de la peticion HTTP

            // Prueba
            var respuesta = await cliente.DeleteAsync($"{url}/{comentario.Id}");

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.NoContent, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Delete_Devuleve403_CuadnoUsuarioIntentaBorrarComentarioDeOtro()
        {
            // Preparacion
            await CrearDataDePrueba();
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false);

            var emailCreadorComentario = "creado-comentario@hotmail.com";
            await CrearUsuario(nombreBD, factory, [], emailCreadorComentario);
            string llaveAPI = await ObtenerAPIKey(nombreBD, factory, emailCreadorComentario, TipoLLave.Profesional); // Se obtiene la llave API

            var context = ConstruirContext(nombreBD);
            var usuarioCreadorComentario = await context.Users.FirstAsync();// el unico usuario que debe existir

            var comentario = new Comentario
            {
                Cuerpo = "contenido",
                UsuarioId = usuarioCreadorComentario!.Id,
                LibroId = 1
            };

            context.Comentarios.Add(comentario);
            await context.SaveChangesAsync();

            var tokenUsuarioDistinto = await CrearUsuario(nombreBD, factory, [], "usuario-distinto@hotmail.com");

            var cliente = factory.CreateClient();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenUsuarioDistinto); // Se asigna el token de otro usuario distinto
            cliente.DefaultRequestHeaders.Add("X-Api-Key", llaveAPI); // Se agrega la llave API en el header de la peticion HTTP

            // Prueba
            var respuesta = await cliente.DeleteAsync($"{url}/{comentario.Id}");// peticion con credenciales invalidas para borrar un comentario

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }
    }
}
