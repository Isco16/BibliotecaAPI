using BibliotecaAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPITests.PruebasUnitarias.Validaciones
{
    [TestClass]
    public class PrimeraLetraMayusculaAttributeTest
    {
        [TestMethod]
        [DataRow("")]
        [DataRow("  ")]
        [DataRow(null)]
        // Se coloca primero el metodo de la clase a probar, luego que se espera que ocurra y finalmente bajp que condicion
        [DataRow("Frank")]
        public void IsValid_RetornaExitoso_SiValueNoTieneLaPrimeraLetraMinuscula(string value) 
        {
            // Perparacion
            var primeraLetraMayusculaAttribute = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object()); // EL parametro que recibe la funcion y que se debe instanciar
            //var value = string.Empty; // el otro parametro que recibe la funcion

            // Prueba
            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext); // Es metodo es distinto al que utiliza la clase pero hace lo mismo

            // Verificacion
            Assert.AreEqual(expected: ValidationResult.Success, actual: resultado); // Clase auxiliar assert para hacer verificaciones
        }

        [TestMethod]
        [DataRow("frank")]
        public void IsValid_RetornaError_SiValueTieneLaPrimeraLetraMinuscula(string value)
        {
            // Perparacion
            var primeraLetraMayusculaAttribute = new PrimeraLetraMayusculaAttribute();
            var validationContext = new ValidationContext(new object()); // EL parametro que recibe la funcion y que se debe instanciar
            //var value = string.Empty; // el otro parametro que recibe la funcion

            // Prueba
            var resultado = primeraLetraMayusculaAttribute.GetValidationResult(value, validationContext); // Es metodo es distinto al que utiliza la clase pero hace lo mismo

            // Verificacion
            Assert.AreEqual(expected: "La primera letra debe estar en mayuscula", actual: resultado!.ErrorMessage); // Clase auxiliar assert para hacer verificaciones
        }
    }
}
