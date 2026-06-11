using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using System.Text.Json;

namespace BibliotecaAPITests.Utilidades
{
    public class BasePruebas
    {
        protected readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true};

        protected readonly Claim adminClaim = new Claim("esAdmin", "1");

        // Metodo auxiliar para crear la base de datos en memoria
        protected ApplicationDbContext ConstruirContext(string nombreBD) // nombre como argumento para usar distintas bases de datos en memoria cuya unica difirencia es el nombre
        {
            var opciones = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(nombreBD).Options;
            var dbContext = new ApplicationDbContext(opciones);
            return dbContext;
        }

        protected IMapper ConfigurarAutoMapper()
        {
            var config = new MapperConfiguration(opciones =>
            {
                opciones.AddProfile(new AutoMapperProfiles());
            }, NullLoggerFactory.Instance);

            return config.CreateMapper();
        }

        // Metodo para cargar app base en memoria para pruebas de integracion
        protected WebApplicationFactory<Program> ConstruirWebApplicationFactory(string nombreBD, bool ignorarSeguridad = true)
        {
            var factory = new WebApplicationFactory<Program>();

            // Config
            factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Evitar 2 proveedores de EntityFramework Core al mismo tiempo siendo ejecutados
                    ServiceDescriptor descriptorDBContext = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))!;

                    // Si encuentra el proveedor SQL SERVER se elimina
                    if (descriptorDBContext is not null)
                    {
                        services.Remove(descriptorDBContext);
                    }

                    services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseInMemoryDatabase(nombreBD));

                    // configuracion reglas de seguridad
                    if (ignorarSeguridad)
                    {
                        services.AddSingleton<IAuthorizationHandler, AllowAnonymousHandler>();

                        services.AddControllers(opciones =>
                        {
                            opciones.Filters.Add(new UsuarioFalsoFiltro());
                        });
                    }
                });
            });

            return factory;
        }

        // Crear usuario sin claims ni email
        protected async Task<string> CrearUsuario(string nombreBD, WebApplicationFactory<Program> factory)
            => await CrearUsuario(nombreBD, factory, [], "ejemplo@hotmail.com");

        // Crear usuario con claims pero sin email
        protected async Task<string> CrearUsuario(string nombreBD, WebApplicationFactory<Program> factory, IEnumerable<Claim> claims)
            => await CrearUsuario(nombreBD, factory, claims, "ejemplo@hotmail.com");

        protected async Task<string> CrearUsuario(string nombreBD, WebApplicationFactory<Program> factory, IEnumerable<Claim> claims, string email)
        {
            var urlRegistro = "/api/v1/usuarios/registro";
            string token = string.Empty;
            token = await ObtenerToken(email, urlRegistro, factory);

            // lisa de claims que se le pasa al crear un usuario
            if (claims.Any())
            {
                var context = ConstruirContext(nombreBD);
                var usuario = await context.Users.Where(x => x.Email == email).FirstAsync();
                Assert.IsNotNull(usuario);

                // Mapear claims al tipo de dato IdentityUserClaim el cual es el tipo de dato que EntityFramework acepta para registrar datos en la tabla de Claims de usuario UserClaims
                var userClaims = claims.Select(x => new IdentityUserClaim<string> // el Select se entindo pot proyeccion
                {
                    UserId = usuario.Id,
                    ClaimType = x.Type,
                    ClaimValue = x.Value
                });

                context.UserClaims.AddRange(userClaims); // Supongo esto es para que el usuario pueda hacer login a la base de datos con Identity
                await context.SaveChangesAsync();
                var urlLogin = "/api/v1/usuarios/login";
                token = await ObtenerToken(email, urlLogin, factory);
            }

            return token;
        }

        private async Task<string> ObtenerToken(string email, string url, WebApplicationFactory<Program> factory)
        {
            var password = "aA123456!";
            var credenciales = new CredencialesUsuarioDTO { Email = email, Password = password };
            var cliente = factory.CreateClient();
            var respuesta = await cliente.PostAsJsonAsync(url, credenciales); // Llamada a la ruta indicada en parametro url
            respuesta.EnsureSuccessStatusCode();

            // Deserializacion de respuesta json a RespuestaAutenticacionDTO
            var contenido = await respuesta.Content.ReadAsStringAsync(); // Obtiene el cuerpo de la respuesta
            var respuestaAutenticacion = JsonSerializer.Deserialize<RespuestaAutenticacionDTO>(contenido, jsonSerializerOptions)!;

            Assert.IsNotNull(respuestaAutenticacion.Token);
            return respuestaAutenticacion.Token;
        }
    }
}
