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
        public int TiempoInactividadLimite { get; set; } = 10; // segundos

        public event Action<ClienteDHCP> ClienteDesactivado;

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
            // Cálculo automático de máscara de red basado en el prefijo
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
            // Verificar si tiene IP reservada
            if (cliente.TieneReserva && !string.IsNullOrEmpty(cliente.IP))
            {
                if (!IPsOcupadas.Contains(cliente.IP))
                {
                    IPsOcupadas.Add(cliente.IP);
                    return cliente.IP;
                }
            }

            // Asignar IP disponible
            for (int i = 2; i < 255; i++) // Saltamos .0 y .1 (red y gateway)
            {
                string ipPosible = $"{RedBase}.{i}";
                if (!IPsOcupadas.Contains(ipPosible) && !IPsReservadas.Contains(ipPosible))
                {
                    IPsOcupadas.Add(ipPosible);
                    cliente.IP = ipPosible;
                    return ipPosible;
                }
            }

            return "No disponible";
        }

        public void ReservarIP(string macAddress, string ip)
        {
            var cliente = Clientes.FirstOrDefault(c => c.MacAddress == macAddress);
            if (cliente != null)
            {
                // Liberar IP anterior si tenía
                if (!string.IsNullOrEmpty(cliente.IP))
                {
                    IPsOcupadas.Remove(cliente.IP);
                }

                cliente.IP = ip;
                cliente.TieneReserva = true;

                if (!IPsReservadas.Contains(ip))
                    IPsReservadas.Add(ip);

                if (!IPsOcupadas.Contains(ip))
                    IPsOcupadas.Add(ip);
            }
        }

        public void LiberarIP(string ip)
        {
            IPsOcupadas.Remove(ip);
            var cliente = Clientes.FirstOrDefault(c => c.IP == ip);
            if (cliente != null)
            {
                cliente.IP = null;
            }
        }

        public void PrevenirIP(string ip)
        {
            if (!IPsReservadas.Contains(ip))
            {
                IPsReservadas.Add(ip);
                // Si está ocupada, liberarla
                if (IPsOcupadas.Contains(ip))
                {
                    LiberarIP(ip);
                }
            }
        }

        private void IniciarVerificacionInactividad()
        {
            timerVerificacionInactividad = new Timer(2000); // Verificar cada 2 segundos
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
                    ClienteDesactivado?.Invoke(cliente);
                }
            }
        }

        public void AgregarClienteEjemplo(int numero)
        {
            // Generar MAC única basada en el número y timestamp para evitar duplicados
            string mac = $"00:1A:2B:3C:{DateTime.Now.Second:D2}:{numero:D2}";
            string hostname = $"Cliente-{numero}";

            var cliente = new ClienteDHCP(mac, hostname);
            cliente.IP = AsignarIP(cliente);
            Clientes.Add(cliente);
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
            RedBase = nuevaRed;
            IPsOcupadas.Clear();

            // Reasignar IPs a todos los clientes basado en la nueva red
            foreach (var cliente in Clientes)
            {
                if (cliente.TieneReserva && !string.IsNullOrEmpty(cliente.IP))
                {
                    // Mantener la reserva si la IP está en el nuevo rango
                    if (cliente.IP.StartsWith(RedBase))
                    {
                        IPsOcupadas.Add(cliente.IP);
                    }
                    else
                    {
                        cliente.IP = AsignarIP(cliente);
                    }
                }
                else
                {
                    cliente.IP = AsignarIP(cliente);
                }
            }
        }
    }
}