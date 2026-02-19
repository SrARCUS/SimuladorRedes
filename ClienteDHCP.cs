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

        private Timer timerTrafico;

        public ClienteDHCP(string mac, string hostname)
        {
            MacAddress = mac;
            Hostname = hostname;
            Activo = true;
            Trafico = 0;
            MedicionActiva = false;
            UltimaActividad = DateTime.Now;
            TieneReserva = false;
        }

        public void IniciarMedicionTrafico(int traficoInicial = 10)
        {
            if (!MedicionActiva)
            {
                MedicionActiva = true;
                Trafico = traficoInicial;

                timerTrafico = new Timer(2000); // Actualizar cada 2 segundos
                timerTrafico.Elapsed += (sender, e) =>
                {
                    if (MedicionActiva && Activo)
                    {
                        // Simular variación de tráfico
                        Random rand = new Random();
                        Trafico = Math.Max(0, Trafico + rand.Next(-5, 6));
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
            if (!Activo)
                Activo = true;
        }
    }
}