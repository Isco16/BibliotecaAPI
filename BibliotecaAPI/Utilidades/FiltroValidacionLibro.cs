using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroValidacionLibro : IAsyncActionFilter
    {
        private readonly ApplicationDbContext dbContext;

        public FiltroValidacionLibro(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if(!context.ActionArguments.TryGetValue("libroCreacionDTO", out var value) || value is not LibroCreacionDTO libroCreacionDTO)
            {
                context.ModelState.AddModelError(string.Empty, "El modelo enviado no es valido");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            if (libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0)
            {
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), "Debe existir al menos un autor para el libro");
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            var autoresIdsExiste = await dbContext.Autores.Where(autor => libroCreacionDTO.AutoresIds
                .Contains(autor.Id))
                .Select(autor => autor.Id).ToListAsync();

            if (autoresIdsExiste.Count != libroCreacionDTO.AutoresIds.Count)
            {
                var autoresIdsNoExiste = libroCreacionDTO.AutoresIds.Except(autoresIdsExiste); // Lista de autoresId que no existen en la base de datos.
                var autoresIdsNoExisteString = string.Join(", ", autoresIdsNoExiste); // Convertir la lista de autoresId que no existen en una cadena separada por comas.
                var mensajeError = $"Los siguientes autores no existen: {autoresIdsNoExisteString}";
                context.ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), mensajeError);
                context.Result = context.ModelState.ConstruirProblemDetail();
                return;
            }

            await next();
        }
    }
}
