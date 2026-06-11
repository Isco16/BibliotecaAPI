namespace BibliotecaAPI.DTOs
{
    // Un record es un tipo de referencia inmutable que proporciona una sintaxis concisa para definir tipos de datos que
    // se utilizan principalmente para almacenar datos.
    // Los records son ideales para representar objetos de valor, como DTOs, ya que proporcionan características como
    // la igualdad basada en el valor y la capacidad de crear copias con modificaciones utilizando la sintaxis with.
    public record PaginacionDTO(int Pagina = 1, int RecordsPorPagina = 10)
    {
        private const int CantidadMaximaRecordsPorPagina = 50;
        // init permite que la propiedad se pueda establecer solo durante la inicialización del objeto, es decir,
        // en el momento de su creación. Una vez que el objeto ha sido creado, las propiedades con init no pueden
        // ser modificadas, lo que garantiza la inmutabilidad de esas propiedades después de la inicialización.
        public int Pagina { get; init; } = Math.Max(1, Pagina);
        public int RecordsPorPagina { get; init; } = Math.Clamp(RecordsPorPagina, 1, CantidadMaximaRecordsPorPagina);
    }
}
