namespace BibliotecaAPI.Entidades
{
    // Entidad de uno a muchos con tabla LlaveAPI
    public class Peticion
    {
        public int Id { get; set; }
        public int LlaveId { get; set; }
        public DateTime FechaPeticion {  get; set; }
        public LlaveAPI? Llave { get; set; }
    }
}
