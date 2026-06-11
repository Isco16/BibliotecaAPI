using BibliotecaAPI.Controllers.V1;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using BibliotecaAPITests.Utilidades;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace BibliotecaAPITests.PruebasUnitarias.Controllers.V1
{
    [TestClass]

    public class UsuariosControllerPruebas: BasePruebas
    {
        private string nombreBD = Guid.NewGuid().ToString();
        private UserManager<Usuario> userManager = null!;
        private SignInManager<Usuario> signInManager = null!;
        private UsuariosController controller = null!;
        private IServicioLlaves servicioLlaves = null!;

        // Metodo que setea todas las dependencias del controlador para poder ser usado en las pruebas
        [TestInitialize]
        public void Setup()
        {
            var context = ConstruirContext(nombreBD);
            // IUserStore: Permite indicar el comportamieno de las ditintas acciones de Identity
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            var miConfiguracion = new Dictionary<string, string>
            {
                {
                    "llavejwt", "jksdkuwdbmankndmklljJHJaawerrsdffsHKJHKLJkljlklfj"
                }
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(miConfiguracion!).Build();

            var contextAccessor = Substitute.For<IHttpContextAccessor>();
            var userClaimsFactory = Substitute.For<IUserClaimsPrincipalFactory<Usuario>>();

            signInManager = Substitute.For<SignInManager<Usuario>>(userManager, contextAccessor, userClaimsFactory, null, null, null, null);

            var servicioUsuarios = Substitute.For<IServiciosUsuarios>();

            var mapper = ConfigurarAutoMapper();

            servicioLlaves = Substitute.For<IServicioLlaves>();

            controller = new UsuariosController(userManager, configuration, signInManager, servicioUsuarios, context, mapper, servicioLlaves);
        }

        [TestMethod]
        public async Task Registrar_DevuelveValidationProblem_CuandoNoEsExitoso()
        {
            // Perparacion
            var mensajeDeError = "prueba";
            var credenciales = new CredencialesUsuarioDTO 
            { 
                Email = "prueba@hotmail.com" , 
                Password = "aA123456!"
            };

            // Al crear cualquier usuario con cualquier password siempre retornara un error de Identity
            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>()).Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "prueba",
                Description = mensajeDeError
            }));

            // Prueba
            var respuesta = await controller.Registrar(credenciales);

            // Verificacion
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: mensajeDeError, problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Registrar_DevuelveToken_CuandoEsExitoso()
        {
            // Perparacion
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            // Al crear cualquier usuario con cualquier password siempre retornara un error de Identity
            userManager.CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>()).Returns(IdentityResult.Success);

            // Prueba
            var respuesta = await controller.Registrar(credenciales);

            // Verificacion
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblem_CuandoUsuarioNoExiste() 
        {
            // Preparacion
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(null!));

            // Prueba
            var respuesta = await controller.Login(credenciales);

            // Validacion
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto", actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveValidationProblem_CuandoLoginEsIncorrecto()
        {
            // Preparacion
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            var usuario = new Usuario { Email = credenciales.Email };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(usuario));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false).Returns(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Prueba
            var respuesta = await controller.Login(credenciales);

            // Validacion
            var resultado = respuesta.Result as ObjectResult;
            var problemDetails = resultado!.Value as ValidationProblemDetails;
            Assert.IsNotNull(problemDetails);
            Assert.AreEqual(expected: 1, problemDetails.Errors.Keys.Count);
            Assert.AreEqual(expected: "Login incorrecto", actual: problemDetails.Errors.Values.First().First());
        }

        [TestMethod]
        public async Task Login_DevuelveToken_CuandoLoginEsCorrecto()
        {
            // Preparacion
            var credenciales = new CredencialesUsuarioDTO
            {
                Email = "prueba@hotmail.com",
                Password = "aA123456!"
            };

            var usuario = new Usuario { Email = credenciales.Email };

            userManager.FindByEmailAsync(credenciales.Email)!.Returns(Task.FromResult<Usuario>(usuario));

            signInManager.CheckPasswordSignInAsync(usuario, credenciales.Password, false).Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Prueba
            var respuesta = await controller.Login(credenciales);

            // Validacion
            Assert.IsNotNull(respuesta.Value);
            Assert.IsNotNull(respuesta.Value.Token);
        }
    }
}
