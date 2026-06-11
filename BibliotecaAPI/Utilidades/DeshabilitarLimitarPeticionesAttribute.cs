namespace BibliotecaAPI.Utilidades
{
    // Restriccion que determina que el atributo se deba usar en metodos y clases y que no este duplicado o mas de una vez en el mismo lugar
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class DeshabilitarLimitarPeticionesAttribute : Attribute
    {

    }
}
