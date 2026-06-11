using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BibliotecaAPITests.Utilidades
{
    // Clase para poder crear usuarios falsos para pruebas de integracion
    public class UsuarioFalsoFiltro : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // ANtes de la accion
            context.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("email","ejemplo@hotmail.com")
            }, "prueba"));

            await next();

            // Despues de la accion
        }
    }
}
