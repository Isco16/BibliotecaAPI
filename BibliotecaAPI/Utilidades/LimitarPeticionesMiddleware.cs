using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BibliotecaAPI.Utilidades
{
    public static class LimitarPeticionesMiddlewareExtensions
    {
        public static IApplicationBuilder UseLimitarPeticiones(this IApplicationBuilder app)
        {
            return app.UseMiddleware<LimitarPeticionesMiddleware>();
        }
    }

    public class LimitarPeticionesMiddleware
    {
        private readonly RequestDelegate next;
        // El middleware es basicamente como un Singleton
        private readonly IOptionsMonitor<LimitarPeticionesDTO> optionsMonitorLimitarPeticiones;

        public LimitarPeticionesMiddleware(RequestDelegate next, IOptionsMonitor<LimitarPeticionesDTO> optionsMonitorLimitarPeticiones)
        {
            this.next = next;
            this.optionsMonitorLimitarPeticiones = optionsMonitorLimitarPeticiones;
        }

        public async Task InvokeAsync(HttpContext httpContext, ApplicationDbContext context)
        {
            var endpoint = httpContext.GetEndpoint();

            if(endpoint is null)
            {
                await next(httpContext);
                return;
            }

            var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>(); // Obtiene metadata del la accion del controlador

            if(actionDescriptor is not null)
            {
                // Verifica si existe alguna instancia del atributo DeshabilitarLimitarPeticionesAttribute a nivel de accion
                var accionTieneAtributoIgnorarLimitarPeticiones = actionDescriptor.MethodInfo
                    .GetCustomAttributes(typeof(DeshabilitarLimitarPeticionesAttribute), inherit: true)
                    .Any();

                // Verifica si existe alguna instancia del atributo DeshabilitarLimitarPeticionesAttribute a nivel de accion
                var controladorTieneAtributoIgnorarLimitarPeticiones = actionDescriptor.ControllerTypeInfo
                    .GetCustomAttributes(typeof(DeshabilitarLimitarPeticionesAttribute), inherit: true)
                    .Any();

                if (accionTieneAtributoIgnorarLimitarPeticiones || controladorTieneAtributoIgnorarLimitarPeticiones)
                {
                    await next(httpContext);
                    return;
                }
            }

            var limitarPeticionesDTO = optionsMonitorLimitarPeticiones.CurrentValue;
            var llaveStringValues = httpContext.Request.Headers["X-Api-Key"]; // Cabecera con el nombre "X-Api-Key"

            if (llaveStringValues.Count == 0)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("Debe proveer la llave en la cabecera X-Api-Key");
                return;
            }

            if(llaveStringValues.Count > 1)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("Solo una llave debe estar presente");
                return;
            }

            var llave = llaveStringValues[0];

            var llaveDB = context.LlavesAPI
                .Include(x => x.RestriccionesDominio)
                .Include(x => x.RestriccionesIP)
                .Include(x => x.Usuario)
                .FirstOrDefault(x => x.Llave == llave);

            if(llaveDB is null)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("La llave no existe");
                return;
            }

            if (!llaveDB.Activa)
            {
                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteAsync("La llave se encuentra inactiva");
                return;
            }

            var restriccionesSuperadas = PeticionSuperaAlgunaDeLasRestricciones(llaveDB, httpContext);

            if (!restriccionesSuperadas)
            {
                httpContext.Response.StatusCode = 403; // Prohibido
                return;
            }

            if(llaveDB.TipoLLave == TipoLLave.Gratuita)
            {
                var hoy = DateTime.UtcNow.Date;
                var cantidadPeticionesRealizadasHoy = await context.Peticiones.CountAsync(x => x.LlaveId == llaveDB.Id && x.FechaPeticion >= hoy);

                if(cantidadPeticionesRealizadasHoy >= limitarPeticionesDTO.PeticionesPorDiaGratuito)
                {
                    httpContext.Response.StatusCode = 429; // Too many request
                    await httpContext.Response.WriteAsync("Ha excedido el limite de peticiones por dia. Si desea realizar mas peticiones, actualice su suscripcion a una cuenta profesional");
                    return;
                }
            } else if (llaveDB.TipoLLave == TipoLLave.Profesional)
            {
                if (llaveDB.Usuario!.MalaPaga)
                {
                    httpContext.Response.StatusCode = 400;
                    await httpContext.Response.WriteAsync("El usuario es un mala paga");
                    return;
                }
            }

            // Podemos hacer la peticion
            var peticion = new Peticion() { LlaveId = llaveDB.Id, FechaPeticion = DateTime.UtcNow};
            context.Add(peticion);
            await context.SaveChangesAsync();

            await next(httpContext);
        }

        private bool PeticionSuperaAlgunaDeLasRestricciones(LlaveAPI llaveAPI, HttpContext httpContext)
        {
            var hayRestricciones = llaveAPI.RestriccionesDominio.Any() || llaveAPI.RestriccionesIP.Any();

            if (!hayRestricciones)
            {
                return true;
            }

            var peticionSuperaLasRestriccionesDeDominio = PeticionSuperaLasRestriccionesDeDominio(llaveAPI.RestriccionesDominio, httpContext);

            var peticionesSuperaLasRestriccionesDeIP = PeticionSuperaLasRestriccionesDeIP(llaveAPI.RestriccionesIP, httpContext);

            return peticionSuperaLasRestriccionesDeDominio || peticionesSuperaLasRestriccionesDeIP;
        }

        private bool PeticionSuperaLasRestriccionesDeDominio(List<RestriccionDominio> restricciones, HttpContext httpContext)
        {
            if(restricciones is null || restricciones.Count == 0)
            {
                return false;
            }

            var referer = httpContext.Request.Headers["referer"].ToString();

            if(referer == string.Empty)
            {
                return false;
            }

            var miUri = new Uri(referer);
            var dominio = miUri.Host;

            var superaRestricion = restricciones.Any(x => x.Dominio == dominio);
            return superaRestricion;
        }

        private bool PeticionSuperaLasRestriccionesDeIP(List<RestriccionIP> restricciones, HttpContext httpContext)
        {
            if(restricciones is null || restricciones.Count == 0)
            {
                return false;
            }

            var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

            if(remoteIpAddress is null)
            {
                return false;
            }

            var IP = remoteIpAddress.ToString();

            if(IP == string.Empty)
            {
                return false;
            }

            var superaRestriccion = restricciones.Any(x => x.IP == IP);
            return superaRestriccion;
        }
    }
}
