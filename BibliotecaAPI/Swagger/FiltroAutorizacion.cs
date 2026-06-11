using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BibliotecaAPI.Swagger
{
    public class FiltroAutorizacion : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>().Any())
            {
                return;
            }

            if (context.ApiDescription.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any())
            {
                return;
            }

            // Codigo para agregar la seguridad a la documentación de Swagger en la version 10+ de Swashbuckle.AspNetCore.SwaggerGen
            operation.Security ??= new List<OpenApiSecurityRequirement>(); // Si la propiedad Security es null, se inicializa como una nueva lista vacía.
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", context.Document)] = []
            });

        }
    }
}
