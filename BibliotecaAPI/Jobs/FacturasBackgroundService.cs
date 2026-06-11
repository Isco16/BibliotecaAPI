using BibliotecaAPI.Datos;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Jobs
{
    public class FacturasBackgroundService: BackgroundService // Servicio para tareas recurrentes
    {
        private readonly IServiceProvider serviceProvider;

        // El ApplicationDbContext es un scoped ya que reprsenta el tiempo de vida del servicio
        public FacturasBackgroundService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        // 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Se obtiene el servicio de ApplicationDbContext
                        Console.WriteLine("Ejecutando proceso de emision de facturas");
                        await EmitirFactures(context); // Se ejecutara esto 1 vez al dia
                        await SetearUsuariosMalaPaga(context);
                        await Task.Delay(TimeSpan.FromDays(1), stoppingToken); // Espera 1 dia o espera a que el token stoppingToken sea ejecutado
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Aqui podemos ejecutar un codigo personalizado al detener la ejecucion del job.
            }

        }

        private async Task SetearUsuariosMalaPaga(ApplicationDbContext context)
        {
            await context.Database.ExecuteSqlAsync($"EXEC Usuarios_SetearMalaPaga"); // El $ es una interpolation para poder ejecutar el codigo en string
        }

        // Se usa el ApplicationDbContext para llamar al procedimiento
        private async Task EmitirFactures(ApplicationDbContext context)
        {

                var hoy = DateTime.Today;
                var fechaComparacion = hoy.AddMonths(-1);

                var facturasDelMesYaFueronEmitidas = await context.FacturasEmitidas.AnyAsync(
                    x => x.Año == fechaComparacion.Year
                    &&
                    x.Mes == fechaComparacion.Month
                    );

                if (!facturasDelMesYaFueronEmitidas)
                {
                    var fechaInicio = new DateTime(fechaComparacion.Year, fechaComparacion.Month, 1); // primer dia del mes pasado
                    var fechaFin = fechaInicio.AddMonths(1);
                    await context.Database.ExecuteSqlAsync(
                        $"EXEC Facturas_Crear {fechaInicio.ToString("yyyy-MM-dd")}, {fechaFin.ToString("yyyy-MM-dd")}"
                    );
                }
        }
    }
}
