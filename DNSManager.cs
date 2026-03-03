using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorRedes
{
    public class DNSManager
    {
        // Propiedades
        public List<DispositivoDNS> Redes { get; private set; }
        public Dictionary<string, string> TablaHosts { get; private set; }

        // Eventos para notificar a DHCP
        public event Action<DispositivoDNS> DispositivoAgregado;
        public event Action<DispositivoDNS> DispositivoEliminado;
        public event Action<DispositivoDNS, string, string> IPCambio; // dispositivo, ipAnterior, ipNueva

        // Constructor
        public DNSManager()
        {
            this.Redes = new List<DispositivoDNS>();
            this.TablaHosts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Métodos públicos
        public void ActualizarTablaHosts()
        {
            this.TablaHosts.Clear();

            foreach (var red in this.Redes)
            {
                var dispositivos = red.ObtenerTodosLosDispositivos();
                foreach (var dispositivo in dispositivos)
                {
                    string fqdn = dispositivo.ObtenerFQDN().ToLower();

                    if (!string.IsNullOrEmpty(dispositivo.IP) && !this.TablaHosts.ContainsKey(fqdn))
                    {
                        this.TablaHosts.Add(fqdn, dispositivo.IP);
                    }
                }
            }
        }

        public DispositivoDNS BuscarPorIdentificador(string identificador)
        {
            if (string.IsNullOrEmpty(identificador))
                return null;

            foreach (var red in this.Redes)
            {
                var dispositivos = red.ObtenerTodosLosDispositivos();
                var encontrado = dispositivos.FirstOrDefault(d => d.IdentificadorTexto == identificador);
                if (encontrado != null)
                    return encontrado;
            }
            return null;
        }

        public DispositivoDNS BuscarPorNombre(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
                return null;

            foreach (var red in this.Redes)
            {
                var dispositivos = red.ObtenerTodosLosDispositivos();
                var encontrado = dispositivos.FirstOrDefault(d =>
                    d.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
                if (encontrado != null)
                    return encontrado;
            }
            return null;
        }

        public DispositivoDNS BuscarPorIP(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return null;

            foreach (var red in this.Redes)
            {
                var dispositivos = red.ObtenerTodosLosDispositivos();
                var encontrado = dispositivos.FirstOrDefault(d => d.IP == ip);
                if (encontrado != null)
                    return encontrado;
            }
            return null;
        }

        public DispositivoDNS BuscarPorMac(string mac)
        {
            if (string.IsNullOrEmpty(mac))
                return null;

            foreach (var red in this.Redes)
            {
                var dispositivos = red.ObtenerTodosLosDispositivos();
                var encontrado = dispositivos.FirstOrDefault(d => d.MacAddress == mac);
                if (encontrado != null)
                    return encontrado;
            }
            return null;
        }

        public string ResolverNombre(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
                return "Nombre vacío";

            nombre = nombre.Trim().ToLower();

            // Búsqueda exacta primero
            if (this.TablaHosts.ContainsKey(nombre))
                return this.TablaHosts[nombre];

            // Si no tiene punto, buscar en el orden específico
            if (!nombre.Contains("."))
            {
                // ORDEN ESPECÍFICO PARA GOOGLE
                string posibleGoogle1 = $"{nombre}.google.com";
                if (this.TablaHosts.ContainsKey(posibleGoogle1))
                    return this.TablaHosts[posibleGoogle1];

                string posibleGoogle2 = $"google.{nombre}.com";
                if (this.TablaHosts.ContainsKey(posibleGoogle2))
                    return this.TablaHosts[posibleGoogle2];

                // ORDEN ESPECÍFICO PARA TECNM
                string posibleTecnm1 = $"{nombre}.mx";
                if (nombre == "tecnm" && this.TablaHosts.ContainsKey(posibleTecnm1))
                    return this.TablaHosts[posibleTecnm1];

                string posibleTecnm2 = $"tecm.{nombre}.mx";
                if (this.TablaHosts.ContainsKey(posibleTecnm2))
                    return this.TablaHosts[posibleTecnm2];

                string posibleTecnm3 = $"tecnm.{nombre}.mx";
                if (this.TablaHosts.ContainsKey(posibleTecnm3))
                    return this.TablaHosts[posibleTecnm3];
            }

            return "No encontrado";
        }

        public List<string> ResolverNombreMultiple(string nombre)
        {
            var resultados = new List<string>();

            if (string.IsNullOrEmpty(nombre))
                return resultados;

            nombre = nombre.Trim().ToLower();

            foreach (var entry in this.TablaHosts)
            {
                if (entry.Key.Contains(nombre))
                {
                    resultados.Add($"{entry.Key} -> {entry.Value}");
                }
            }

            return resultados;
        }

        public void AgregarDispositivo(DispositivoDNS padre, DispositivoDNS nuevo)
        {
            if (nuevo == null)
                return;

            if (padre == null)
            {
                this.Redes.Add(nuevo);
            }
            else
            {
                padre.AgregarHijo(nuevo);
            }

            ActualizarTablaHosts();

            // Notificar a DHCP que se agregó un dispositivo
            DispositivoAgregado?.Invoke(nuevo);
        }

        public bool EliminarDispositivo(DispositivoDNS dispositivo)
        {
            if (dispositivo == null)
                return false;

            bool eliminado = false;

            if (dispositivo.Padre == null)
            {
                eliminado = this.Redes.Remove(dispositivo);
            }
            else if (dispositivo.Padre != null)
            {
                eliminado = dispositivo.Padre.Hijos.Remove(dispositivo);
            }

            if (eliminado)
            {
                ActualizarTablaHosts();
                // Notificar a DHCP que se eliminó el dispositivo
                DispositivoEliminado?.Invoke(dispositivo);
            }

            return eliminado;
        }

        public void ActualizarIP(DispositivoDNS dispositivo, string nuevaIP)
        {
            if (dispositivo == null || string.IsNullOrEmpty(nuevaIP))
                return;

            string ipAnterior = dispositivo.IP;
            dispositivo.IP = nuevaIP;
            ActualizarTablaHosts();

            // Notificar cambio de IP
            IPCambio?.Invoke(dispositivo, ipAnterior, nuevaIP);
        }

        public List<DispositivoDNS> ObtenerTodosLosDispositivos()
        {
            var todos = new List<DispositivoDNS>();
            foreach (var red in this.Redes)
            {
                todos.AddRange(red.ObtenerTodosLosDispositivos());
            }
            return todos;
        }

        public void LimpiarTodo()
        {
            this.Redes.Clear();
            this.TablaHosts.Clear();
        }

        public int ContarDispositivos()
        {
            return ObtenerTodosLosDispositivos().Count;
        }

        public List<string> ObtenerDominios()
        {
            var dominios = new List<string>();
            foreach (var red in this.Redes)
            {
                var dispositivos = red.ObtenerTodosLosDispositivos();
                foreach (var dispositivo in dispositivos)
                {
                    if (!string.IsNullOrEmpty(dispositivo.Dominio) && !dominios.Contains(dispositivo.Dominio))
                    {
                        dominios.Add(dispositivo.Dominio);
                    }
                }
            }
            return dominios;
        }

        public void ImportarDesdeDHCP(List<ClienteDHCP> clientesDHCP)
        {
            if (clientesDHCP == null || !clientesDHCP.Any())
                return;

            LimpiarTodo();

            // Crear estructura base
            var google = new DispositivoDNS(
                "google",
                "10.0.0.1",
                "00:00:00:00:01:00",
                "ORG-GGL-001"
            );
            google.TipoOrganizacion = TipoOrganizacion.Empresa;
            google.Dominio = "google.com";

            var tecnm = new DispositivoDNS(
                "tecnm",
                "192.168.1.1",
                "00:00:00:03:00:00",
                "ORG-TNM-001"
            );
            tecnm.TipoOrganizacion = TipoOrganizacion.Universidad;
            tecnm.Dominio = "tecnm.mx";

            // Procesar clientes DHCP para construir la estructura
            foreach (var cliente in clientesDHCP)
            {
                if (cliente.Dominio == "google.com")
                {
                    if (cliente.Hostname == "marketing")
                    {
                        var marketing = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            $"DHCP-{cliente.MacAddress.Replace(":", "")}"
                        );
                        marketing.TipoOrganizacion = TipoOrganizacion.Empresa;
                        marketing.Dominio = "google.com";
                        google.AgregarHijo(marketing);
                    }
                    else if (cliente.Hostname == "compras")
                    {
                        var compras = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            $"DHCP-{cliente.MacAddress.Replace(":", "")}"
                        );
                        compras.TipoOrganizacion = TipoOrganizacion.Empresa;
                        compras.Dominio = "google.com";
                        google.AgregarHijo(compras);
                    }
                    else
                    {
                        // Es una PC, buscar su departamento
                        var dispositivo = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            $"DHCP-{cliente.MacAddress.Replace(":", "")}"
                        );
                        dispositivo.TipoOrganizacion = TipoOrganizacion.Empresa;
                        dispositivo.Dominio = "google.com";

                        // Asignar al departamento correspondiente
                        if (cliente.Hostname.Contains("ana") || cliente.Hostname.Contains("luis"))
                        {
                            var marketing = google.Hijos.FirstOrDefault(h => h.Nombre == "marketing");
                            if (marketing == null)
                            {
                                marketing = new DispositivoDNS(
                                    "marketing",
                                    "10.0.1.1",
                                    "00:00:00:01:00:00",
                                    "DEPT-MKT-001"
                                );
                                marketing.TipoOrganizacion = TipoOrganizacion.Empresa;
                                marketing.Dominio = "google.com";
                                google.AgregarHijo(marketing);
                            }
                            marketing.AgregarHijo(dispositivo);
                        }
                        else if (cliente.Hostname.Contains("carlos") || cliente.Hostname.Contains("maria"))
                        {
                            var compras = google.Hijos.FirstOrDefault(h => h.Nombre == "compras");
                            if (compras == null)
                            {
                                compras = new DispositivoDNS(
                                    "compras",
                                    "10.0.2.1",
                                    "00:00:00:02:00:00",
                                    "DEPT-CMP-001"
                                );
                                compras.TipoOrganizacion = TipoOrganizacion.Empresa;
                                compras.Dominio = "google.com";
                                google.AgregarHijo(compras);
                            }
                            compras.AgregarHijo(dispositivo);
                        }
                    }
                }
                else if (cliente.Dominio == "tecnm.mx")
                {
                    if (cliente.Hostname == "isc")
                    {
                        var isc = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            $"DHCP-{cliente.MacAddress.Replace(":", "")}"
                        );
                        isc.TipoOrganizacion = TipoOrganizacion.Universidad;
                        isc.Dominio = "tecnm.mx";
                        tecnm.AgregarHijo(isc);
                    }
                    else if (cliente.Hostname == "ige")
                    {
                        var ige = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            $"DHCP-{cliente.MacAddress.Replace(":", "")}"
                        );
                        ige.TipoOrganizacion = TipoOrganizacion.Universidad;
                        ige.Dominio = "tecnm.mx";
                        tecnm.AgregarHijo(ige);
                    }
                    else
                    {
                        // Es un servidor o laboratorio
                        var dispositivo = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            $"DHCP-{cliente.MacAddress.Replace(":", "")}"
                        );
                        dispositivo.TipoOrganizacion = TipoOrganizacion.Universidad;
                        dispositivo.Dominio = "tecnm.mx";

                        // Asignar al departamento correspondiente
                        if (cliente.Hostname.Contains("isc"))
                        {
                            var isc = tecnm.Hijos.FirstOrDefault(h => h.Nombre == "isc");
                            if (isc == null)
                            {
                                isc = new DispositivoDNS(
                                    "isc",
                                    "192.168.2.1",
                                    "00:00:00:04:00:00",
                                    "DEPT-ISC-001"
                                );
                                isc.TipoOrganizacion = TipoOrganizacion.Universidad;
                                isc.Dominio = "tecnm.mx";
                                tecnm.AgregarHijo(isc);
                            }
                            isc.AgregarHijo(dispositivo);
                        }
                        else if (cliente.Hostname.Contains("ige"))
                        {
                            var ige = tecnm.Hijos.FirstOrDefault(h => h.Nombre == "ige");
                            if (ige == null)
                            {
                                ige = new DispositivoDNS(
                                    "ige",
                                    "192.168.3.1",
                                    "00:00:00:05:00:00",
                                    "DEPT-IGE-001"
                                );
                                ige.TipoOrganizacion = TipoOrganizacion.Universidad;
                                ige.Dominio = "tecnm.mx";
                                tecnm.AgregarHijo(ige);
                            }
                            ige.AgregarHijo(dispositivo);
                        }
                    }
                }
            }

            this.Redes.Add(google);
            this.Redes.Add(tecnm);

            ActualizarTablaHosts();
        }
    }
}