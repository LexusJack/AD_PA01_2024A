using System.Linq;  // Importa métodos de extensión de LINQ para manipulación de colecciones

namespace Protocolo  // Espacio de nombres para definir el protocolo de comunicación
{
    // Clase que representa una solicitud (pedido) en el protocolo de comunicación
    public class Pedido
    {
        // Propiedad para almacenar el comando de la solicitud
        public string Comando { get; set; }

        // Propiedad para almacenar los parámetros de la solicitud
        public string[] Parametros { get; set; }

        // Método estático para procesar un mensaje de texto y convertirlo en un objeto Pedido
        public static Pedido Procesar(string mensaje)
        {
            // Divide el mensaje en partes usando espacios como separador
            var partes = mensaje.Split(' ');

            // Crea y devuelve un nuevo objeto Pedido
            return new Pedido
            {
                // El primer elemento es el comando (convertido a mayúsculas)
                Comando = partes[0].ToUpper(),
                // El resto de los elementos son los parámetros
                Parametros = partes.Skip(1).ToArray()
            };
        }

        // Sobrescribe el método ToString para representar el pedido como cadena
        public override string ToString()
        {
            // Combina el comando y los parámetros en una sola cadena
            return $"{Comando} {string.Join(" ", Parametros)}";
        }
    }

    // Clase que representa una respuesta en el protocolo de comunicación
    public class Respuesta
    {
        // Propiedad para almacenar el estado de la respuesta (OK/NOK)
        public string Estado { get; set; }

        // Propiedad para almacenar el mensaje de la respuesta
        public string Mensaje { get; set; }

        // Sobrescribe el método ToString para representar la respuesta como cadena
        public override string ToString()
        {
            // Combina el estado y el mensaje en una sola cadena
            return $"{Estado} {Mensaje}";
        }
    }
}
