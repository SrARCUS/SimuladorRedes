using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;

namespace SimuladorRedes
{
    public class DHCPManager
    {
        public List<ClienteDHCP> Clientes { get; private set; }
        public string RedBase { get; private set; }
        public int PrefijoRed { get; private set; }
        public string MascaraRed { get; private set; }
        public List<string> IPsReservadas { get; private set; }
        public List<string> IPsOcupadas { get; private set; }

        private Timer timerVerificacionInactividad;
        public int TiempoInactividadLimite { get; set; } = 10;

        // Eventos para notificar cambios
        public event Action<ClienteDHCP> ClienteDesactivado;
        public event Action<ClienteDHCP> ClienteAgregado;
        public event Action<ClienteDHCP> ClienteEliminado;
        public event Action<ClienteDHCP, string, string> IPCambio;

        public DHCPManager()
        {
            Clientes = new List<ClienteDHCP>();
            IPsReservadas = new List<string>();
            IPsOcupadas = new List<string>();
            RedBase = "192.168.1";
            PrefijoRed = 24;
            CalcularMascaraRed();

            IniciarVerificacionInactividad();
        }

        private void CalcularMascaraRed()
        {
            uint mascaraBits = 0xFFFFFFFF << (32 - PrefijoRed);
            byte[] bytes = BitConverter.GetBytes(mascaraBits);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            MascaraRed = new IPAddress(bytes).ToString();
        }

        public void CambiarPrefijoRed(int nuevoPrefijo)
        {
            if (nuevoPrefijo >= 8 && nuevoPrefijo <= 30)
            {
                PrefijoRed = nuevoPrefijo;
                CalcularMascaraRed();
            }
        }

        public string AsignarIP(ClienteDHCP cliente)
        {
            string redBase = !string.IsNullOrEmpty(cliente.RedBase) ? cliente.RedBase : RedBase;

            if (cliente.TieneReserva && !string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
            {
                if (!IPsOcupadas.Contains(cliente.IP))
                {
                    IPsOcupadas.Add(cliente.IP);
                    return cliente.IP;
                }
            }

            for (int i = 2; i < 255; i++)
            {
                string ipPosible = $"{redBase}.{i}";
                if (!IPsOcupadas.Contains(ipPosible) && !IPsReservadas.Contains(ipPosible))
                {
                    IPsOcupadas.Add(ipPosible);
                    cliente.IP = ipPosible;
                    return ipPosible;
                }
            }

            return "No disponible";
        }

        public void AgregarCliente(string hostname, string ip, string mac, string dominio, string organizacion, string redBase)
        {
            var cliente = new ClienteDHCP(mac, hostname, dominio, organizacion, redBase);
            cliente.IP = ip;
            cliente.Manager = this;
            Clientes.Add(cliente);
            IPsOcupadas.Add(ip);

            ClienteAgregado?.Invoke(cliente);
        }

        public void AgregarClienteEjemplo(string hostname, string ip, string mac, string dominio, string organizacion, string redBase)
        {
            AgregarCliente(hostname, ip, mac, dominio, organizacion, redBase);
        }

        public bool EliminarCliente(ClienteDHCP cliente)
        {
            if (cliente == null) return false;

            if (Clientes.Contains(cliente))
            {
                if (!string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
                {
                    IPsOcupadas.Remove(cliente.IP);
                }

                cliente.Desactivar();
                Clientes.Remove(cliente);

                ClienteEliminado?.Invoke(cliente);
                return true;
            }
            return false;
        }

        public bool EliminarClientePorHostname(string hostname)
        {
            var cliente = Clientes.FirstOrDefault(c => c.Hostname == hostname);
            if (cliente != null)
            {
                return EliminarCliente(cliente);
            }
            return false;
        }

        public void ActualizarIP(ClienteDHCP cliente, string nuevaIP)
        {
            if (cliente == null || string.IsNullOrEmpty(nuevaIP))
                return;

            string ipAnterior = cliente.IP;

            if (!string.IsNullOrEmpty(ipAnterior) && ipAnterior != "No asignada")
            {
                IPsOcupadas.Remove(ipAnterior);
            }

            cliente.IP = nuevaIP;
            IPsOcupadas.Add(nuevaIP);

            IPCambio?.Invoke(cliente, ipAnterior, nuevaIP);
        }

        public void ReservarIP(string macAddress, string ip)
        {
            var cliente = Clientes.FirstOrDefault(c => c.MacAddress == macAddress);
            if (cliente != null)
            {
                if (!string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
                {
                    IPsOcupadas.Remove(cliente.IP);
                }

                string ipAnterior = cliente.IP;
                cliente.IP = ip;
                cliente.TieneReserva = true;

                if (!IPsReservadas.Contains(ip))
                    IPsReservadas.Add(ip);

                if (!IPsOcupadas.Contains(ip))
                    IPsOcupadas.Add(ip);

                IPCambio?.Invoke(cliente, ipAnterior, ip);
            }
        }

        public void LiberarIP(string ip, ClienteDHCP cliente = null)
        {
            if (cliente != null)
            {
                if (cliente.TieneReserva)
                    return;

                string ipAnterior = cliente.IP;
                cliente.IP = "No asignada";
                IPCambio?.Invoke(cliente, ipAnterior, "No asignada");
            }
            else
            {
                var clienteConIP = Clientes.FirstOrDefault(c => c.IP == ip);
                if (clienteConIP != null && !clienteConIP.TieneReserva)
                {
                    string ipAnterior = clienteConIP.IP;
                    clienteConIP.IP = "No asignada";
                    IPCambio?.Invoke(clienteConIP, ipAnterior, "No asignada");
                }
            }

            IPsOcupadas.Remove(ip);
        }

        public void PrevenirIP(string ip)
        {
            if (!IPsReservadas.Contains(ip))
            {
                IPsReservadas.Add(ip);
                if (IPsOcupadas.Contains(ip))
                {
                    LiberarIP(ip);
                }
            }
        }

        private void IniciarVerificacionInactividad()
        {
            timerVerificacionInactividad = new Timer(2000);
            timerVerificacionInactividad.Elapsed += VerificarInactividad;
            timerVerificacionInactividad.Start();
        }

        private void VerificarInactividad(object sender, ElapsedEventArgs e)
        {
            foreach (var cliente in Clientes.Where(c => c.Activo))
            {
                var tiempoInactivo = DateTime.Now - cliente.UltimaActividad;
                if (tiempoInactivo.TotalSeconds > TiempoInactividadLimite)
                {
                    cliente.Activo = false;
                    cliente.DetenerMedicionTrafico();

                    if (!cliente.TieneReserva && !string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
                    {
                        LiberarIP(cliente.IP, cliente);
                    }

                    ClienteDesactivado?.Invoke(cliente);
                }
            }
        }

        public void DetenerTodosLosTimers()
        {
            timerVerificacionInactividad?.Stop();
            timerVerificacionInactividad?.Dispose();

            foreach (var cliente in Clientes)
            {
                cliente.DetenerMedicionTrafico();
            }
        }

        public void CambiarRedBase(string nuevaRed)
        {
            // Este método ahora está obsoleto porque cada cliente tiene su propia red
        }
        public void NotificarIPCambio(ClienteDHCP cliente, string ipAnterior, string ipNueva)
        {
            IPCambio?.Invoke(cliente, ipAnterior, ipNueva);
        }
    }
}