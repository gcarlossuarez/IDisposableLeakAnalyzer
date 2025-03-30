using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FindClassesThatDoNotDisposeResource.ValidationException
{
    public class DynamicInstantiator
    {
        private const string VALIDATION_EXCEPTION_CLASS = "ValidationExceptionClass";
        /// <summary>
        /// Get an instance for the ValidationException class from the App.config configuration file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TypeLoadException"></exception>
        public static ValidationException CreateInstanceFromConfig()
        {
            // Get the class name from the App.config configuration file
            string className = ConfigurationManager.AppSettings[VALIDATION_EXCEPTION_CLASS];

            // If the key does not exist, it takes the default value of an instance of the
            // ValidationException class.
            if (string.IsNullOrEmpty(className))
            {
                return new ValidationException();
            }

            // Obtener el ensamblado actual (donde se encuentran las clases)
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Crear una instancia del tipo
            Type type = assembly.GetType(className);

            if (type == null)
            {
                //throw new TypeLoadException($"No se pudo encontrar el tipo: {className}. Revise que el valor de la clave '{VALIDATION_EXCEPTION_CLASS}, incluya el espacio de nombres.'");
                Console.ForegroundColor = ConsoleColor.Red;
                string message = Messages.Get("ValidationExceptionClassWrongDefined", className,
                    VALIDATION_EXCEPTION_CLASS);
                Console.WriteLine(message);
                Console.ResetColor();
                System.Threading.Thread.Sleep(3000);

                throw new TypeLoadException(message);
                return new ValidationException();
            }

            // Create an instance of the object
            return (ValidationException)Activator.CreateInstance(type);
        }
    }
}
