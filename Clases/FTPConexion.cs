using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorRedes.Clases
{
    public class FTPConexion
    {
        public ClienteDHCP Cliente { get; }
        public ClienteDHCP Servidor { get; }
        public FTPUsuario Usuario { get; }
        public DateTime HoraConexion { get; }
        public bool Activa { get; set; }

        public FTPConexion(ClienteDHCP cliente, ClienteDHCP servidor, FTPUsuario usuario)
        {
            Cliente = cliente;
            Servidor = servidor;
            Usuario = usuario;
            HoraConexion = DateTime.Now;
            Activa = true;
        }

        public override string ToString() =>
            $"{Cliente.Hostname} ({Cliente.IP}) → {Servidor.Hostname} ({Servidor.IP})  [{Usuario.Username}]";
    }
}
