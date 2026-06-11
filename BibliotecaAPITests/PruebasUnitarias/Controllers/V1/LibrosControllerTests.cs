

using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPITests.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;

namespace BibliotecaAPITests.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class LibrosControllerTests: BasePruebas
    {
        [TestMethod]
        public async Task Get_RetornarCeroLibros_CuandoNoHayLibros()
        {
            // Preparacion
            var nombreBD = Guid.NewGuid().ToString();
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutoMapper();
            IOutputCacheStore outputCacheStore = null!;

            var controller = new LibrosController(context, mapper, outputCacheStore);

            controller.ControllerContext.HttpContext = new DefaultHttpContext();

            var paginacionDTO = new PaginacionDTO(1, 1);

            // Prueba

            var respuesta = await controller.Get(paginacionDTO);

            // Verificacion
            Assert.HasCount(expected: 0, collection: respuesta);
        }
    }
}
