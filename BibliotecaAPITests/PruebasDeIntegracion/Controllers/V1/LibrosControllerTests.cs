using BibliotecaAPI.DTOs;
using BibliotecaAPITests.Utilidades;
using System.Net;

namespace BibliotecaAPITests.PruebasDeIntegracion.Controllers.V1
{
    [TestClass]
    public class LibrosControllerTests: BasePruebas
    {
        private readonly string url = "/api/v1/libros";
        private string nombreBD = Guid.NewGuid().ToString();

        [TestMethod]
        public async Task Post_Devuelve400_CuandoAutoresIdsEsVacio()
        {
            // Preparacion
            var factory = ConstruirWebApplicationFactory(nombreBD);
            var cliente = factory.CreateClient();
            var libroCreaionDTO = new LibroCreacionDTO { Titulo = "titulo" };

            // Prueba
            var respuesta = await cliente.PostAsJsonAsync(url, libroCreaionDTO);

            // Verificacion
            Assert.AreEqual(expected: HttpStatusCode.BadRequest, actual: respuesta.StatusCode);
        }
    }
}
