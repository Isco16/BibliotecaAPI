using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using System.Security.Claims;

namespace BibliotecaAPITests.PruebasUnitarias.Servicios
{
    [TestClass]
    public class ServicioUsuariosTests
    {
        private UserManager<Usuario> userManager = null!;
        private IHttpContextAccessor contextAccessor = null!;
        private ServiciosUsuarios servicioUsuarios = null!;

        [TestInitialize]
        public void Setup()
        {
            // IUserStore: Permite indicar el comportamieno de las ditintas acciones de Identity
            userManager = Substitute.For<UserManager<Usuario>>(
                Substitute.For<IUserStore<Usuario>>(), null, null, null, null, null, null, null, null);

            contextAccessor = Substitute.For<IHttpContextAccessor>();
            servicioUsuarios = new ServiciosUsuarios(userManager, contextAccessor);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoNoHayClaimEmail() 
        {
            // Preparacion
            var httpContext = new DefaultHttpContext();
            contextAccessor.HttpContext.Returns(httpContext); // MEtodo Returns obliga al contexdaccessor a que al acceder a la propiedad HttpContext siempre devuelva el argumento que se le entrega (httpContext).

            // Prueba
            var usuario = await servicioUsuarios.ObtenerUsuario();

            // Verificacion
            Assert.IsNull(usuario);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarUsuario_CuandoHayClaimEmail()
        {
            // Preparacion
            var email = "prueba@hotmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult(usuarioEsperado)); // Returns indica en este caso que al llamar a FindByEmailAsync(email) devuelva una tarea con resultado exitoso adjuntando usuaroEsperadp

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims}; // Se declara e inicializa un HttpContext asignando claims al usuario en cuastion
            contextAccessor.HttpContext.Returns(httpContext); // MEtodo Returns obliga al contexdaccessor a que al acceder a la propiedad HttpContext siempre devuelva el argumento que se le entrega (httpContext).

            // Prueba
            var usuario = await servicioUsuarios.ObtenerUsuario();

            // Verificacion
            Assert.IsNotNull(usuario);
            Assert.AreEqual(expected: email, actual: usuario.Email);
        }

        [TestMethod]
        public async Task ObtenerUsuario_RetornarNulo_CuandoUsuarioNoExiste()
        {
            // Preparacion
            var email = "prueba@hotmail.com";
            var usuarioEsperado = new Usuario { Email = email };

            userManager.FindByEmailAsync(email)!.Returns(Task.FromResult<Usuario>(null!)); // Returns indica en este caso que al llamar a FindByEmailAsync(email) devuelva una tarea con resultado exitoso adjuntando un usuario nulo

            var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("email", email)
            }));

            var httpContext = new DefaultHttpContext() { User = claims }; // Se declara e inicializa un HttpContext asignando claims al usuario en cuastion
            contextAccessor.HttpContext.Returns(httpContext); // MEtodo Returns obliga al contexdaccessor a que al acceder a la propiedad HttpContext siempre devuelva el argumento que se le entrega (httpContext).

            // Prueba
            var usuario = await servicioUsuarios.ObtenerUsuario();

            // Verificacion
            Assert.IsNull(usuario);
        }
    }
}
