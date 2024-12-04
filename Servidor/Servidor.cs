using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Protocolo;  // Namespace personalizado para protocolo de comunicación

namespace Servidor  // Namespace del servidor
{
    class Servidor  // Clase principal del servidor
    {
        // Listener de TCP para aceptar conexiones
        private static TcpListener escuchador;

        // Diccionario para rastrear el número de solicitudes por cliente
        private static Dictionary<string, int> listadoClientes
            = new Dictionary<string, int>();

        // Método principal de entrada del programa
        static void Main(string[] args)
        {
            try
            {
                // Inicializa el listener en cualquier dirección IP, puerto 8080
                escuchador = new TcpListener(IPAddress.Any, 8080);
                escuchador.Start();
                Console.WriteLine("Servidor inició en el puerto 5000...");

                // Bucle infinito para aceptar conexiones de clientes
                while (true)
                {
                    // Acepta una conexión de cliente
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado, puerto: {0}", cliente.Client.RemoteEndPoint.ToString());
                    
                    // Crea un hilo para manejar cada cliente
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                // Manejo de errores de socket al iniciar el servidor
                Console.WriteLine("Error de socket al iniciar el servidor: " +
                    ex.Message);
            }
            finally 
            {
                // Detiene el listener al finalizar
                escuchador?.Stop();
            }
        }

        // Método para manejar la comunicación con un cliente
        private static void ManipuladorCliente(object obj)
        {
            // Convierte el objeto a TcpClient
            TcpClient cliente = (TcpClient)obj;
            NetworkStream flujo = null;
            try
            {
                // Obtiene el flujo de red del cliente
                flujo = cliente.GetStream();
                byte[] bufferTx;
                byte[] bufferRx = new byte[1024];
                int bytesRx;

                // Bucle para leer mensajes del cliente
                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    // Convierte los bytes recibidos a cadena
                    string mensajeRx =
                        Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
                    
                    // Procesa el mensaje entrante
                    Pedido pedido = Pedido.Procesar(mensajeRx);
                    Console.WriteLine("Se recibio: " + pedido);

                    // Obtiene la dirección del cliente
                    string direccionCliente =
                        cliente.Client.RemoteEndPoint.ToString();
                    
                    // Resuelve el pedido y genera una respuesta
                    Respuesta respuesta = ResolverPedido(pedido, direccionCliente);
                    Console.WriteLine("Se envió: " + respuesta);

                    // Envía la respuesta al cliente
                    bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }

            }
            catch (SocketException ex)
            {
                // Manejo de errores de socket
                Console.WriteLine("Error de socket al manejar el cliente: " + ex.Message);
            }
            finally
            {
                // Cierra los recursos de red
                flujo?.Close();
                cliente?.Close();
            }
        }

        // Método para procesar diferentes tipos de solicitudes
        private static Respuesta ResolverPedido(Pedido pedido, string direccionCliente)
        {
            // Respuesta por defecto en caso de comando no reconocido
            Respuesta respuesta = new Respuesta
            { Estado = "NOK", Mensaje = "Comando no reconocido" };

            // Manejo de diferentes comandos
            switch (pedido.Comando)
            {
                case "INGRESO":
                    // Validación de credenciales (usuario y contraseña fijos)
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        // Acceso aleatorio (simulando un sistema de autenticación)
                        respuesta = new Random().Next(2) == 0
                            ? new Respuesta 
                            { Estado = "OK", 
                                Mensaje = "ACCESO_CONCEDIDO" }
                            : new Respuesta 
                            { Estado = "NOK", 
                                Mensaje = "ACCESO_NEGADO" };
                    }
                    else
                    {
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                case "CALCULO":
                    // Procesamiento de cálculo de día de circulación
                    if (pedido.Parametros.Length == 3)
                    {
                        string modelo = pedido.Parametros[0];
                        string marca = pedido.Parametros[1];
                        string placa = pedido.Parametros[2];
                        
                        // Valida la placa
                        if (ValidarPlaca(placa))
                        {
                            // Obtiene el día de circulación según la placa
                            byte indicadorDia = ObtenerIndicadorDia(placa);
                            respuesta = new Respuesta
                            { Estado = "OK", 
                                Mensaje = $"{placa} {indicadorDia}" };
                            
                            // Registra la solicitud del cliente
                            ContadorCliente(direccionCliente);
                        }
                        else
                        {
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                case "CONTADOR":
                    // Devuelve el número de solicitudes para un cliente
                    if (listadoClientes.ContainsKey(direccionCliente))
                    {
                        respuesta = new Respuesta
                        { Estado = "OK",
                            Mensaje = listadoClientes[direccionCliente].ToString() };
                    }
                    else
                    {
                        respuesta.Mensaje = "No hay solicitudes previas";
                    }
                    break;
            }

            return respuesta;
        }

        // Método para validar el formato de la placa
        private static bool ValidarPlaca(string placa)
        {
            // Expresión regular: 3 letras mayúsculas seguidas de 4 dígitos
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$");
        }

        // Método para determinar el día de circulación según el último dígito de la placa
        private static byte ObtenerIndicadorDia(string placa)
        {
            // Extrae el último dígito de la placa
            int ultimoDigito = int.Parse(placa.Substring(6, 1));
            
            // Asigna un día de circulación según el dígito
            switch (ultimoDigito)
            {
                case 1: 
                case 2: 
                    return 0b00100000; // Lunes
                case 3: 
                case 4: 
                    return 0b00010000; // Martes
                case 5: 
                case 6: 
                    return 0b00001000; // Miércoles
                case 7: 
                case 8: 
                    return 0b00000100; // Jueves
                case 9: 
                case 0: 
                    return 0b00000010; // Viernes
                default: 
                    return 0;
            }
        }

        // Método para contar las solicitudes por cliente
        private static void ContadorCliente(string direccionCliente)
        {
            // Incrementa o inicializa el contador de solicitudes
            if (listadoClientes.ContainsKey(direccionCliente))
            {
                listadoClientes[direccionCliente]++;
            }
            else
            {
                listadoClientes[direccionCliente] = 1;
            }
        }
    }
}
