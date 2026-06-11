using Microsoft.AspNetCore.Authorization;

namespace BibliotecaAPITests.Utilidades
{
    // Clase para ignorar reglas de seguridad para realizar pruebas de integracion
    public class AllowAnonymousHandler : IAuthorizationHandler
    {
        // Se ignoran todas las reglas de seguridad
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var requirement in context.PendingRequirements)
            {
                    context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
