using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorRedes
{
    public enum TipoOrganizacion
    {
        Empresa,
        Universidad,
        Gobierno,
        Hogar,
        Otro
    }

    public class DispositivoDNS
    {
        // Propiedades
        public string Nombre { get; set; }
        public string IP { get; set; }
        public string MacAddress { get; set; }
        public string IdentificadorTexto { get; set; }
        public List<DispositivoDNS> Hijos { get; private set; }
        public DispositivoDNS Padre { get; set; }
        public TipoOrganizacion TipoOrganizacion { get; set; }
        public string Dominio { get; set; }

        // Constructor
        public DispositivoDNS(string nombre, string ip, string mac, string identificador)
        {
            this.Nombre = nombre;
            this.IP = ip;
            this.MacAddress = mac;
            this.IdentificadorTexto = identificador;
            this.Hijos = new List<DispositivoDNS>();
            this.TipoOrganizacion = TipoOrganizacion.Otro;
            this.Dominio = "local";
            this.Padre = null;
        }

        // Métodos
        public string ObtenerRutaCompleta()
        {
            if (this.Padre == null)
                return this.Nombre;
            else
                return $"{this.Padre.ObtenerRutaCompleta()}.{this.Nombre}";
        }

        public string ObtenerFQDN()
        {
            if (this.Padre == null)
            {
                // Es un dominio principal: google.com, tecnm.mx
                return $"{this.Nombre}.{this.Dominio}";
            }
            else
            {
                // Tiene padre: marketing.google.com, pc-ana.marketing.google.com
                return $"{ObtenerRutaCompleta()}.{this.Dominio}";
            }
        }

        public void AgregarHijo(DispositivoDNS hijo)
        {
            if (hijo != null)
            {
                hijo.Padre = this;
                this.Hijos.Add(hijo);
            }
        }

        public bool EliminarHijo(DispositivoDNS hijo)
        {
            if (hijo != null && this.Hijos.Contains(hijo))
            {
                hijo.Padre = null;
                return this.Hijos.Remove(hijo);
            }
            return false;
        }

        public List<DispositivoDNS> ObtenerTodosLosDispositivos()
        {
            var dispositivos = new List<DispositivoDNS> { this };
            foreach (var hijo in this.Hijos)
            {
                dispositivos.AddRange(hijo.ObtenerTodosLosDispositivos());
            }
            return dispositivos;
        }

        public bool TieneHijos()
        {
            return this.Hijos.Count > 0;
        }

        public bool EsRaiz()
        {
            return this.Padre == null;
        }

        public bool EsHoja()
        {
            return this.Hijos.Count == 0;
        }

        public int ObtenerNivel()
        {
            int nivel = 0;
            DispositivoDNS actual = this.Padre;
            while (actual != null)
            {
                nivel++;
                actual = actual.Padre;
            }
            return nivel;
        }

        public override string ToString()
        {
            return $"{this.Nombre} ({this.IP}) - {this.TipoOrganizacion}";
        }

        public override bool Equals(object obj)
        {
            if (obj is DispositivoDNS otro)
            {
                return this.IdentificadorTexto == otro.IdentificadorTexto;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.IdentificadorTexto?.GetHashCode() ?? 0;
        }
    }
}