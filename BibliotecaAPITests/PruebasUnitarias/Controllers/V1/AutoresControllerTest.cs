using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Servicios.V1;
using BibliotecaAPITests.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BibliotecaAPITests.PruebasUnitarias.Controllers.V1
{
    [TestClass]
    public class AutoresControllerTest : BasePruebas
    {
        IAlmacenadorArchivos almacenadorArchivos = null!;
        ILogger<AutoresController> logger = null!;
        IOutputCacheStore outputCacheStore = null!;
        IServicioAutores servicioAutores = null!;
        private string nombreBD = Guid.NewGuid().ToString();
        private AutoresController controller = null!;

        [TestInitialize]
        public void Setup()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            var mapper = ConfigurarAutoMapper();
            almacenadorArchivos = Substitute.For<IAlmacenadorArchivos>();
            logger = Substitute.For<ILogger<AutoresController>>();
            outputCacheStore = Substitute.For<IOutputCacheStore>();
            servicioAutores = Substitute.For<IServicioAutores>();

            controller = new AutoresController(context, mapper, almacenadorArchivos, logger, outputCacheStore, servicioAutores);
        }

        [TestMethod]
        public async Task Get_Retorna404_CuandoAutorConIdNoExiste()
        {
            // Preparacion

            // Prueba
            var respuesta = await controller.Get(1);

            // Verificacion
            var resultado = respuesta.Result as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Get_RetornaAutor_CuandoAutorConIdExiste()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            // Se crea 2 autores sin usar el metodo Post solo para evitar tener que mezclar otros metodos de prueba. La idea es enfocarse solo en el metodo en cuestion (GET).
            context.Autores.Add(new Autor { Nombres = "Felipe", Apellidos = "Gavilan" });
            context.Autores.Add(new Autor { Nombres = "Claudia", Apellidos = "Rodriguez" });

            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Get(1);

            // Verificacion
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
        }

        [TestMethod]
        public async Task Get_RetornaAutor_CuandoAutorTieneLibros()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);

            var libro1 = new Libro { Titulo = "Libro 1" };
            var libro2 = new Libro { Titulo = "Libro 2" };

            var autor = new Autor()
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Libros = new List<AutorLibro>
                {
                    new AutorLibro{Libro = libro1},
                    new AutorLibro{Libro = libro2}
                }
            };

            context.Add(autor);

            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Get(1);

            // Verificacion
            var resultado = respuesta.Value;
            Assert.AreEqual(expected: 1, actual: resultado!.Id);
            Assert.AreEqual(expected: 2, actual: resultado.Libros.Count);
            Assert.HasCount(expected: 2, resultado.Libros);
        }

        [TestMethod]
        public async Task Get_DebeLLamarGetDelServicioAutores()
        {
            // Preparacion
            var paginacionDTO = new PaginacionDTO(2, 3);

            // Prueba
            await controller.Get(paginacionDTO);

            // Verificacion
            await servicioAutores.Received(1).Get(paginacionDTO); // Linea que verifica si el Get de IServicioAutores fue llamado 1 vez y que tambien se le haya pasado en argumento correcto.
        }

        [TestMethod]
        public async Task Post_DebeCrearAutor_CuandoEnviamosAutor()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);

            var nuevoAutor = new AutorCreacionDTO { Nombres = "nuevo", Apellidos = "autor" };

            // Prueba
            var respuesta = await controller.Post(nuevoAutor);

            // Verificacion
            var resultado = respuesta as CreatedAtRouteResult;
            Assert.IsNotNull(resultado); // Primera verificacion

            var context2 = ConstruirContext(nombreBD); // Evita trabajar con un contexto contaminado
            var cantidad = await context2.Autores.CountAsync();

            Assert.AreEqual(expected: 1, actual: cantidad);

        }

        [TestMethod]
        public async Task Put_Retorna404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO: null);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        private const string contenedor = "autores";
        private const string cache = "autores-obtener";

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoEnviamosAutorSinFoto()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "Id"
            });

            await context.SaveChangesAsync();
            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Felipe2",
                Apellidos = "Gavilan2",
                Identificacion = "Id2"
            };

            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync(); // devuelve el unico elemento de una secuencia. Error si no existe unicamente 1 elemento.

            Assert.AreEqual(expected: "Felipe2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Gavilan2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.DidNotReceiveWithAnyArgs().Editar(default, default!, default!);
        }

        [TestMethod]
        public async Task Put_ActualizarAutor_CuandoEnviamosAutorConFoto()
        {
            // Preparacion
            var context = ConstruirContext(nombreBD);

            var urlAnterior = "URL-1";
            var urlNueva = "URL-2";
            almacenadorArchivos.Editar(default, default!, default!).ReturnsForAnyArgs(urlNueva); // Metodo de Substitute para simular que cuando se llame a Editar siempre duvuelva el mismo valor (urlNueva) con cualquier argumento entregado.

            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "Id",
                Foto = urlAnterior
            });

            await context.SaveChangesAsync();

            var formFile = Substitute.For<IFormFile>();

            var autorCreacionDTO = new AutorCreacionDTOConFoto
            {
                Nombres = "Felipe2",
                Apellidos = "Gavilan2",
                Identificacion = "Id2",
                Foto = formFile
            };

            // Prueba
            var respuesta = await controller.Put(1, autorCreacionDTO);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode);

            var context3 = ConstruirContext(nombreBD);
            var autorActualizado = await context3.Autores.SingleAsync(); // devuelve el unico elemento de una secuencia. Error si no existe unicamente 1 elemento.

            Assert.AreEqual(expected: "Felipe2", actual: autorActualizado.Nombres);
            Assert.AreEqual(expected: "Gavilan2", actual: autorActualizado.Apellidos);
            Assert.AreEqual(expected: "Id2", actual: autorActualizado.Identificacion);
            Assert.AreEqual(expected: urlNueva, actual: autorActualizado.Foto);
            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Editar(urlAnterior, contenedor, formFile);
        }

        [TestMethod]
        public async Task Patch_Retorna400_CuandoPatchDocEsNulo()
        {
            // Prueba
            var respuesta = await controller.Patch(1, patchDocument: null!);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 400, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Patch_Retorna404_CuandoAutorNoExsite()
        {
            // Peparacion
            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 404, actual: resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Patch_RetornaValidationProblem_CuandoHayErrorDeValidacion()
        {
            // Peparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>(); // Para que el controlador no de un error se debe pasar un ObjectModelValidator pero por medio de SUbstitute
            controller.ObjectValidator = objectValidator;

            var mensajeDeError = "mensaje de error";
            controller.ModelState.AddModelError("", mensajeDeError);

            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);

            // Verificacion
            var resultado = respuesta as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, actual: problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Patch_ActualizaUnCampo_CuandoSeLeEnviaUnaOperacion()
        {
            // Peparacion
            var context = ConstruirContext(nombreBD);
            context.Autores.Add(new Autor
            {
                Nombres = "Felipe",
                Apellidos = "Gavilan",
                Identificacion = "123",
                Foto = "URL-1"
            });

            await context.SaveChangesAsync();

            var objectValidator = Substitute.For<IObjectModelValidator>(); // Para que el controlador no de un error se debe pasar un ObjectModelValidator pero por medio de SUbstitute
            controller.ObjectValidator = objectValidator;

            var patchDoc = new JsonPatchDocument<AutorPatchDTO>();
            patchDoc.Operations.Add(new Operation<AutorPatchDTO>("replace", "/nombres", null, "Felipe2"));

            // Prueba
            var respuesta = await controller.Patch(1, patchDoc);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(expected: 204, resultado!.StatusCode);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);

            var context3 = ConstruirContext(nombreBD);
            var autorBD = await context3.Autores.SingleAsync();

            Assert.AreEqual(expected: "Felipe2", autorBD.Nombres);
            Assert.AreEqual(expected: "Gavilan", autorBD.Apellidos);
            Assert.AreEqual(expected: "123", autorBD.Identificacion);
            Assert.AreEqual(expected: "URL-1", autorBD.Foto);
        }

        [TestMethod]
        public async Task Delete_Retorna404_CuandoAutorNoExiste()
        {
            // Prueba
            var respuesta = await controller.Delete(1);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(404, resultado!.StatusCode);
        }

        [TestMethod]
        public async Task Delete_BorraAutor_CuandoAutorExiste()
        {
            // Preapracion
            var urlFoto = "URL-1";

            var context = ConstruirContext(nombreBD);

            context.Autores.Add(new Autor { Nombres = "Autor1", Apellidos = "Autor1", Foto = urlFoto});
            context.Autores.Add(new Autor { Nombres = "Autor2", Apellidos = "Autor2"});

            await context.SaveChangesAsync();

            // Prueba
            var respuesta = await controller.Delete(1);

            // Verificacion
            var resultado = respuesta as StatusCodeResult;
            Assert.AreEqual(204, resultado!.StatusCode); // 204: No content

            var context2 = ConstruirContext(nombreBD);
            var cantidadAutores = await context2.Autores.CountAsync();
            Assert.AreEqual(expected: 1, actual: cantidadAutores);

            var autorExiste = await context.Autores.AnyAsync(x => x.Nombres == "Autor2");
            Assert.IsTrue(autorExiste);

            await outputCacheStore.Received(1).EvictByTagAsync(cache, default);
            await almacenadorArchivos.Received(1).Borrar(urlFoto, contenedor);
        }
    }
}
