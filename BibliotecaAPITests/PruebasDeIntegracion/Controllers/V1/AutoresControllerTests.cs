using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPITests.Utilidades;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace BibliotecaAPITests.PruebasDeIntegracion.Controllers.V1
{
    // Clase de preuba de integracion para realizar peticiones desde un cliente HTTP en el controlador de autores
    [TestClass]
    public class AutoresControllerTests: BasePruebas
    {
        public static readonly string url = "/api/v1/autores";
        private string nombreBD = Guid.NewGuid().ToString();


        [TestMethod]
        public async Task Get_Devuelve404_CuandoAutorNoExiste()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD);


            var claims = new List<Claim> { adminClaim };

            string email = "ejemplo@hotmail.com";

            // Ahora se le envia la lista de claims
            var token = await CrearUsuario(nombreBD, factory, claims, email);

            string llaveAPI = await ObtenerAPIKey(nombreBD, factory, email, TipoLLave.Profesional); // Se obtiene la llave API
            var cliente = factory.CreateClient(); // Se crea el cliente HTTP para realiazr peticiones
            cliente.DefaultRequestHeaders.Add("X-Api-Key", llaveAPI); // Se agrega la llave API en el header de la peticion HTTP

            // Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificacion
            var statusCode = respuesta.StatusCode;
            Assert.AreEqual(expected: HttpStatusCode.NotFound, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Get_DevuelveAutor_CuandoAutorExiste()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor() { Nombres="Felipe", Apellidos="Gavilan"});
            context.Autores.Add(new Autor() { Nombres="Claudia", Apellidos="Rodriguez"});
            await context.SaveChangesAsync();

            var factory = ConstruirWebApplicationFactory(nombreBD);

            var claims = new List<Claim> { adminClaim };

            string email = "ejemplo@hotmail.com";

            // Ahora se le envia la lista de claims
            var token = await CrearUsuario(nombreBD, factory, claims, email);

            string llaveAPI = await ObtenerAPIKey(nombreBD, factory, email, TipoLLave.Profesional); // Se obtiene la llave API
            var cliente = factory.CreateClient(); // Se crea el cliente HTTP para realiazr peticiones
            cliente.DefaultRequestHeaders.Add("X-Api-Key", llaveAPI); // Se agrega la llave API en el header de la peticion HTTP

            // Prueba
            var respuesta = await cliente.GetAsync($"{url}/1");

            // Verificacion
            respuesta.EnsureSuccessStatusCode(); // Verifica que se tuvo una respuesa tipo 200

            var autor = JsonSerializer.Deserialize<AutorConLibrosDTO>(
                await respuesta.Content.ReadAsStringAsync(), jsonSerializerOptions)!;

            Assert.AreEqual(expected: 1, autor.Id);
        }

        [TestMethod]
        public async Task Post_Devuelve401_CuandoUsuarioNoEstaAutenticado() 
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false); // No se quiere ignorar las reglas de seguridad
            var cliente = factory.CreateClient(); // Se crea el cliente HTTP para realiazr peticiones

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Unauthorized, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve403_CuandoUsuarioNoEsAdmin()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false); // No se quiere ignorar las reglas de seguridad
            var token = await CrearUsuario(nombreBD, factory);
            
            var cliente = factory.CreateClient(); // Se crea el cliente HTTP para realiazr peticiones

            cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.Forbidden, actual: respuesta.StatusCode);
        }

        [TestMethod]
        public async Task Post_Devuelve403_CuandoUsuarioEsAdmin()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD, ignorarSeguridad: false); // No se quiere ignorar las reglas de seguridad

            var claims = new List<Claim> { adminClaim };

            string email = "ejemplo@hotmail.com";

            // Ahora se le envia la lista de claims
            var token = await CrearUsuario(nombreBD, factory, claims, email);

            string llaveAPI = await ObtenerAPIKey(nombreBD, factory, email, TipoLLave.Profesional); // Se obtiene la llave API
            var cliente = factory.CreateClient(); // Se crea el cliente HTTP para realiazr peticiones
            cliente.DefaultRequestHeaders.Add("X-Api-Key", llaveAPI); // Se agrega la llave API en el header de la peticion HTTP

            cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);


            var autorCreacionDTO = new AutorCreacionDTO
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, autorCreacionDTO);

            // Verificacion
            respuesta.EnsureSuccessStatusCode();
            Assert.AreEqual(expected: HttpStatusCode.Created, actual: respuesta.StatusCode);
        }
    }
}
