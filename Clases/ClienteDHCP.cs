using System;
using System.Timers;

namespace SimuladorRedes
{
    public class ClienteDHCP
    {
        public string MacAddress { get; set; }
        public string IP { get; set; }
        public string Hostname { get; set; }
        public bool Activo { get; set; }
        public int Trafico { get; set; } // En Mbps
        public bool MedicionActiva { get; set; }
        public DateTime UltimaActividad { get; set; }
        public bool TieneReserva { get; set; }

        // Propiedades para DNS
        public string Dominio { get; set; }
        public string Organizacion { get; set; }
        public string RedBase { get; set; }

        private Timer timerTrafico;
        public DHCPManager Manager { get; set; }

        public ClienteDHCP(string mac, string hostname, string dominio = "local", string organizacion = "Otro", string redBase = "192.168.1")
        {
            MacAddress = mac;
            Hostname = hostname;
            Dominio = dominio;
            Organizacion = organizacion;
            RedBase = redBase;
            Activo = true;
            Trafico = 0;
            MedicionActiva = false;
            UltimaActividad = DateTime.Now;
            TieneReserva = false;
        }

        public void IniciarMedicionTrafico(int traficoInicial = 10)
        {
            if (!MedicionActiva && Activo)
            {
                MedicionActiva = true;
                Trafico = Math.Max(0, Math.Min(100, traficoInicial));

                timerTrafico = new Timer(2000);
                timerTrafico.Elapsed += (sender, e) =>
                {
                    if (MedicionActiva && Activo)
                    {
                        Random rand = new Random();
                        int nuevoTrafico = Trafico + rand.Next(-5, 6);
                        Trafico = Math.Max(0, Math.Min(100, nuevoTrafico));
                        UltimaActividad = DateTime.Now;
                    }
                };
                timerTrafico.Start();
            }
        }

        public void DetenerMedicionTrafico()
        {
            MedicionActiva = false;
            Trafico = 0;
            if (timerTrafico != null)
            {
                timerTrafico.Stop();
                timerTrafico.Dispose();
            }
        }

        public void ActualizarActividad()
        {
            UltimaActividad = DateTime.Now;
        }

        public void Activar()
        {
            if (!Activo)
            {
                Activo = true;
                UltimaActividad = DateTime.Now;

                if (string.IsNullOrEmpty(IP) || IP == "No asignada")
                {
                    if (Manager != null)
                    {
                        IP = Manager.AsignarIP(this);
                    }
                }
            }
        }

        public void Desactivar()
        {
            if (Activo)
            {
                Activo = false;
                DetenerMedicionTrafico();

                if (!TieneReserva && !string.IsNullOrEmpty(IP) && Manager != null)
                {
                    Manager.LiberarIP(IP, this);
                    IP = "No asignada";
                }
            }
        }

        public string ObtenerFQDN()
        {
            return $"{Hostname}.{Dominio}";
        }

        public string ObtenerClaseIP()
        {
            if (string.IsNullOrEmpty(IP) || IP == "No asignada")
                return "Sin IP";

            string[] partes = IP.Split('.');
            if (partes.Length == 4)
            {
                int primerOcteto = int.Parse(partes[0]);
                if (primerOcteto >= 1 && primerOcteto <= 126)
                    return "Clase A";
                else if (primerOcteto >= 128 && primerOcteto <= 191)
                    return "Clase B";
                else if (primerOcteto >= 192 && primerOcteto <= 223)
                    return "Clase C";
            }
            return "Desconocida";
        }
        public override string ToString() => Hostname;
    }
}