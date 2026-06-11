using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaAPI.Migrations
{
    /// <inheritdoc />
    public partial class CreaStoredProcedure_Facturas_Crear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aqui se coloca el procedure
            migrationBuilder.Sql(@"
CREATE PROCEDURE Facturas_Crear
	-- Add the parameters for the stored procedure here
	@fechaInicio datetime,
	@fechaFin datetime
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	DECLARE @montoPorCadaPeticion decimal(4,4) = 1.0/2 -- 1 dolar por cada 2 peticiones

INSERT INTO Facturas(UsuarioId, Monto, FechaEmision, FechaLimiteDePago, Pagada)
SELECT 
UsuarioId, -- Id usuario
COUNT(*) * @montoPorCadaPeticion AS Monto, -- Calculo de monto a pagar
GETDATE() AS FechaEmision, -- Fecha de emision de fatcura
DATEADD(d, 60, GETDATE()) as FechaLimiteDePago, -- Fecha limite (se toma un maximo de 60 dias despues de la emision)
0 as Pagada -- La factura comienza como falso ya que no esta pagada
FROM Peticiones 
INNER JOIN LlavesAPI
ON LlavesAPI.Id = Peticiones.LlaveId
WHERE LlavesAPI.TipoLlave != 1 AND FechaPeticion >= @fechaInicio AND FechaPeticion < @fechaFin
GROUP BY LlavesAPI.UsuarioId
;

INSERT INTO FacturasEmitidas(Mes, Año)
SELECT
	CASE MONTH(GetDate()) -- Parecido a un Switch en C#
	WHEN 1 then 12 -- Si es enero se le coloca el mes anterior el cual corresponde a diciembre cuyo numero es 12
	ELSE MONTH(GetDate()) - 1 END AS Mes,

	CASE MONTH(GetDate())
	WHEN 1 then YEAR(GETDATE()) - 1 -- si en enero se le resta un año
	ELSE YEAR(GETDATE()) END AS Mes
;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Aqui se ejecuta el siguiente codigo en caso de revertir la migracion
            migrationBuilder.Sql("DROP PROCEDURE Facturas_Crear");
        }
    }
}
