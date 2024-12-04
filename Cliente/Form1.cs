using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using Protocolo;  // Espacio de nombres de protocolo personalizado

namespace Cliente  // Espacio de nombres del cliente
{
    public partial class FrmValidador : Form  // Formulario de validación
    {
        // Cliente TCP y flujo de red para comunicación
        private TcpClient remoto;
        private NetworkStream flujo;

        // Constructor
        public FrmValidador()
        {
            InitializeComponent();
        }

        // Controlador de eventos de carga del formulario
        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Intenta establecer conexión TCP a localhost en el puerto 8080
                remoto = new TcpClient("127.0.0.1", 8080);
                flujo = remoto.GetStream();
            }
            catch (SocketException ex)
            {
                // Muestra mensaje de error si la conexión falla
                MessageBox.Show("No se pudo establecer conexión " + ex.Message, "ERROR");
            }
            finally 
            {
                // Asegura que los recursos de red se cierren
                flujo?.Close();
                remoto?.Close();
            }

            // Deshabilita las casillas de días hasta que el inicio de sesión sea exitoso
            panPlaca.Enabled = false;
            chkLunes.Enabled = false;
            chkMartes.Enabled = false;
            chkMiercoles.Enabled = false;
            chkJueves.Enabled = false;
            chkViernes.Enabled = false;
            chkDomingo.Enabled = false;
            chkSabado.Enabled = false;
        }

        // Controlador de eventos de clic en botón de inicio de sesión
        private void btnIniciar_Click(object sender, EventArgs e)
        {
            // Valida que el nombre de usuario y contraseña no estén vacíos
            string usuario = txtUsuario.Text;
            string contraseña = txtPassword.Text;
            if (usuario == "" || contraseña == "")
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña", "ADVERTENCIA");
                return;
            }

            // Crea paquete de solicitud de inicio de sesión
            Pedido pedido = new Pedido
            {
                Comando = "INGRESO",
                Parametros = new[] { usuario, contraseña }
            };
            
            // Envía solicitud de inicio de sesión y obtiene respuesta
            Respuesta respuesta = HazOperacion(pedido);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Maneja la respuesta de inicio de sesión
            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
            {
                // Inicio de sesión exitoso: habilita panel de placa, deshabilita panel de inicio de sesión
                panPlaca.Enabled = true;
                panLogin.Enabled = false;
                MessageBox.Show("Acceso concedido", "INFORMACIÓN");
                txtModelo.Focus();
            }
            else if (respuesta.Estado == "NOK" && respuesta.Mensaje == "ACCESO_NEGADO")
            {
                // Inicio de sesión fallido: mantiene panel de inicio de sesión habilitado, muestra error
                panPlaca.Enabled = false;
                panLogin.Enabled = true;
                MessageBox.Show("No se pudo ingresar, revise credenciales", "ERROR");
                txtUsuario.Focus();
            }
        }

        // Método para enviar solicitudes y recibir respuestas
        private Respuesta HazOperacion(Pedido pedido)
        {
            // Verifica si existe el flujo de red
            if(flujo == null)
            {
                MessageBox.Show("No hay conexión", "ERROR");
                return null;
            }
            try
            {
                // Convierte solicitud a bytes y envía
                byte[] bufferTx = Encoding.UTF8.GetBytes(
                    pedido.Comando + " " + string.Join(" ", pedido.Parametros));
                
                flujo.Write(bufferTx, 0, bufferTx.Length);

                // Recibe respuesta
                byte[] bufferRx = new byte[1024];
                
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length);
                
                // Convierte respuesta a cadena
                string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
                
                // Analiza respuesta
                var partes = mensaje.Split(' ');
                
                return new Respuesta
                {
                    Estado = partes[0],
                    Mensaje = string.Join(" ", partes.Skip(1).ToArray())
                };
            }
            catch (SocketException ex)
            {
                MessageBox.Show("Error al intentar transmitir " + ex.Message, "ERROR");
            }
            finally 
            {
                // Cierra recursos de red
                flujo?.Close();
                remoto?.Close();
            }
            return null;
        }

        // Controlador de eventos de clic en botón de consulta
        private void btnConsultar_Click(object sender, EventArgs e)
        {
            // Obtiene detalles del vehículo
            string modelo = txtModelo.Text;
            string marca = txtMarca.Text;
            string placa = txtPlaca.Text;
            
            // Crea solicitud de cálculo
            Pedido pedido = new Pedido
            {
                Comando = "CALCULO",
                Parametros = new[] { modelo, marca, placa }
            };
            
            // Envía solicitud y obtiene respuesta
            Respuesta respuesta = HazOperacion(pedido);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Maneja la respuesta
            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
                // Desmarca todas las casillas de días
                chkLunes.Checked = false;
                chkMartes.Checked = false;
                chkMiercoles.Checked = false;
                chkJueves.Checked = false;
                chkViernes.Checked = false;
            }
            else
            {
                // Analiza y muestra respuesta
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("Se recibió: " + respuesta.Mensaje, "INFORMACIÓN");
                
                // Usa operación de bits para determinar día permitido
                byte resultado = Byte.Parse(partes[1]);
                switch (resultado)
                {
                    // Diferentes patrones de bits representan diferentes días
                    case 0b00100000:
                        chkLunes.Checked = true;
                        break;
                    case 0b00010000:
                        chkMartes.Checked = true;
                        break;
                    case 0b00001000:
                        chkMiercoles.Checked = true;
                        break;
                    case 0b00000100:
                        chkJueves.Checked = true;
                        break;
                    case 0b00000010:
                        chkViernes.Checked = true;
                        break;
                    default:
                        // Desmarca todo si no se encuentra un día válido
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                }
            }
        }

        // Controlador de eventos de clic en botón de número de consultas
        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            // Envía un mensaje simple "hola" para obtener el conteo de solicitudes
            String mensaje = "hola";
            
            Pedido pedido = new Pedido
            {
                Comando = "CONTADOR",
                Parametros = new[] { mensaje }
            };

            // Envía solicitud y obtiene respuesta
            Respuesta respuesta = HazOperacion(pedido);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error", "ERROR");
                return;
            }

            // Muestra número de solicitudes
            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
            }
            else
            {
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("El número de pedidos recibidos en este cliente es " + partes[0],
                    "INFORMACIÓN");
            }
        }

        // Controlador de eventos de cierre del formulario
        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Asegura que los recursos de red se cierren cuando se cierra el formulario
            if (flujo != null)
                flujo.Close();
            if (remoto != null)
                if (remoto.Connected)
                    remoto.Close();
        }
    }
}
