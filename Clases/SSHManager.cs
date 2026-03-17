using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorRedes
{
    // ─────────────────────────────────────────────────────────────
    //  Representa una sesión SSH activa entre un cliente y servidor
    // ─────────────────────────────────────────────────────────────
    public class SSHConexion
    {
        public ClienteDHCP Cliente { get; set; }
        public ClienteDHCP Servidor { get; set; }
        public DateTime HoraConexion { get; private set; }
        public bool Activa { get; set; }

        public SSHConexion(ClienteDHCP cliente, ClienteDHCP servidor)
        {
            Cliente = cliente;
            Servidor = servidor;
            HoraConexion = DateTime.Now;
            Activa = true;
        }

        public override string ToString() =>
            $"{Cliente.Hostname} ({Cliente.IP})  →  {Servidor.Hostname} ({Servidor.IP})  [{HoraConexion:HH:mm:ss}]";
    }

    // ─────────────────────────────────────────────────────────────
    //  Gestiona los sistemas de archivos de todos los clientes y
    //  las conexiones SSH entre ellos
    // ─────────────────────────────────────────────────────────────
    public class SSHManager
    {
        // hostname → carpeta raíz del cliente
        private readonly Dictionary<string, SSHCarpeta> _sistemasArchivos;

        public List<SSHConexion> HistorialConexiones { get; private set; }
        public SSHConexion ConexionActiva { get; private set; }

        // Eventos para logging externo
        public event Action<string> AccionRealizada;

        public SSHManager()
        {
            _sistemasArchivos = new Dictionary<string, SSHCarpeta>(StringComparer.OrdinalIgnoreCase);
            HistorialConexiones = new List<SSHConexion>();
        }

        // ── Inicialización del sistema de archivos ────────────────

        /// <summary>
        /// Crea una carpeta raíz para cada cliente DHCP (si aún no tiene).
        /// Añade un readme.txt de ejemplo.
        /// </summary>
        public void InicializarClientesSistemaArchivos(IEnumerable<ClienteDHCP> clientes)
        {
            foreach (var cliente in clientes)
                ObtenerCarpetaRaiz(cliente);   // crea si no existe
        }

        /// <summary>Devuelve (o crea) la carpeta raíz del cliente.</summary>
        public SSHCarpeta ObtenerCarpetaRaiz(ClienteDHCP cliente)
        {
            if (!_sistemasArchivos.TryGetValue(cliente.Hostname, out var raiz))
            {
                raiz = new SSHCarpeta(cliente.Hostname);
                raiz.CrearArchivo("readme", $"Directorio raíz de {cliente.Hostname}\nIP: {cliente.IP}\nDominio: {cliente.Dominio}");
                _sistemasArchivos[cliente.Hostname] = raiz;
            }
            return raiz;
        }

        // ── Gestión de conexiones SSH ─────────────────────────────

        /// <summary>
        /// Establece una conexión SSH. Lanza excepción si no es posible.
        /// </summary>
        public SSHConexion Conectar(ClienteDHCP cliente, ClienteDHCP servidor)
        {
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            if (servidor == null) throw new ArgumentNullException(nameof(servidor));

            if (!cliente.Activo)
                throw new InvalidOperationException($"El cliente '{cliente.Hostname}' no está activo.");

            if (!servidor.Activo)
                throw new InvalidOperationException($"El servidor '{servidor.Hostname}' no está activo.");

            if (cliente.Hostname.Equals(servidor.Hostname, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Un cliente no puede conectarse a sí mismo.");

            // Cerrar conexión previa si existe
            if (ConexionActiva != null)
                Desconectar();

            // Asegurar que el servidor tiene sistema de archivos
            ObtenerCarpetaRaiz(servidor);

            var conexion = new SSHConexion(cliente, servidor);
            HistorialConexiones.Add(conexion);
            ConexionActiva = conexion;

            string msg = $"SSH CONNECT: {cliente.Hostname} ({cliente.IP}) → {servidor.Hostname} ({servidor.IP})";
            Logger.Log(msg);
            NotificarAccion($"✅ Conectado: {cliente.Hostname} → {servidor.Hostname}");

            return conexion;
        }

        /// <summary>Cierra la conexión SSH activa.</summary>
        public void Desconectar()
        {
            if (ConexionActiva == null) return;

            string msg = $"SSH DISCONNECT: {ConexionActiva.Cliente.Hostname} ↔ {ConexionActiva.Servidor.Hostname}  (duración: {(DateTime.Now - ConexionActiva.HoraConexion):mm\\:ss})";
            ConexionActiva.Activa = false;
            Logger.Log(msg);
            NotificarAccion($"⭕ Desconectado: {ConexionActiva.Cliente.Hostname} ↔ {ConexionActiva.Servidor.Hostname}");
            ConexionActiva = null;
        }

        // ── Operaciones LOCALES (no requieren conexión activa) ────

        public SSHCarpeta CrearCarpetaLocal(ClienteDHCP cliente, string nombre, SSHCarpeta padre = null)
        {
            SSHCarpeta destino = padre ?? ObtenerCarpetaRaiz(cliente);
            var nueva = destino.CrearSubcarpeta(nombre)
                        ?? throw new InvalidOperationException($"Ya existe una carpeta llamada '{nombre}'.");
            Logger.LogDHCP("SSH-LOCAL", cliente.Hostname, cliente.IP, $"Carpeta creada: {nueva.ObtenerRuta()}");
            NotificarAccion($"📁 Carpeta local '{nombre}' creada en {cliente.Hostname}");
            return nueva;
        }

        public bool EliminarCarpetaLocal(SSHCarpeta carpeta)
        {
            if (carpeta.EsRaiz())
                throw new InvalidOperationException("No se puede eliminar la carpeta raíz.");

            bool ok = carpeta.Padre.EliminarSubcarpeta(carpeta.Nombre);
            if (ok) NotificarAccion($"🗑️ Carpeta local '{carpeta.Nombre}' eliminada");
            return ok;
        }

        public SSHArchivo CrearArchivoLocal(ClienteDHCP cliente, string nombre, SSHCarpeta carpeta = null, string contenido = "")
        {
            SSHCarpeta destino = carpeta ?? ObtenerCarpetaRaiz(cliente);
            var archivo = destino.CrearArchivo(nombre, contenido)
                          ?? throw new InvalidOperationException($"Ya existe un archivo llamado '{nombre}.txt'.");
            Logger.LogDHCP("SSH-LOCAL", cliente.Hostname, cliente.IP, $"Archivo creado: {archivo.Nombre}");
            NotificarAccion($"📄 Archivo local '{archivo.Nombre}' creado");
            return archivo;
        }

        public bool EliminarArchivoLocal(SSHArchivo archivo, SSHCarpeta carpetaPadre)
        {
            bool ok = carpetaPadre.EliminarArchivo(archivo.Nombre);
            if (ok) NotificarAccion($"🗑️ Archivo local '{archivo.Nombre}' eliminado");
            return ok;
        }

        // ── Operaciones en SERVIDOR (requieren conexión activa) ───

        private void VerificarConexion()
        {
            if (ConexionActiva == null)
                throw new InvalidOperationException("No hay ninguna conexión SSH activa.");
        }

        public SSHCarpeta CrearCarpetaEnServidor(string nombre, SSHCarpeta padre = null)
        {
            VerificarConexion();
            SSHCarpeta raiz = ObtenerCarpetaRaiz(ConexionActiva.Servidor);
            SSHCarpeta destino = padre ?? raiz;

            var nueva = destino.CrearSubcarpeta(nombre)
                        ?? throw new InvalidOperationException($"Ya existe una carpeta llamada '{nombre}' en el servidor.");

            string detalle = $"Carpeta remota creada: {nueva.ObtenerRuta()} por {ConexionActiva.Cliente.Hostname}";
            Logger.LogDHCP("SSH-REMOTO", ConexionActiva.Servidor.Hostname, ConexionActiva.Servidor.IP, detalle);
            NotificarAccion($"📁 Carpeta '{nombre}' creada en {ConexionActiva.Servidor.Hostname}");
            return nueva;
        }

        public bool EliminarCarpetaEnServidor(SSHCarpeta carpeta)
        {
            VerificarConexion();
            if (carpeta.EsRaiz())
                throw new InvalidOperationException("No se puede eliminar la carpeta raíz del servidor.");

            bool ok = carpeta.Padre.EliminarSubcarpeta(carpeta.Nombre);
            if (ok)
            {
                Logger.LogDHCP("SSH-REMOTO", ConexionActiva.Servidor.Hostname, ConexionActiva.Servidor.IP,
                    $"Carpeta eliminada: {carpeta.Nombre} por {ConexionActiva.Cliente.Hostname}");
                NotificarAccion($"🗑️ Carpeta '{carpeta.Nombre}' eliminada de {ConexionActiva.Servidor.Hostname}");
            }
            return ok;
        }

        public SSHArchivo CrearArchivoEnServidor(string nombre, SSHCarpeta carpeta = null, string contenido = "")
        {
            VerificarConexion();
            SSHCarpeta raiz = ObtenerCarpetaRaiz(ConexionActiva.Servidor);
            SSHCarpeta destino = carpeta ?? raiz;

            var archivo = destino.CrearArchivo(nombre, contenido)
                          ?? throw new InvalidOperationException($"Ya existe un archivo llamado '{nombre}.txt' en el servidor.");

            Logger.LogDHCP("SSH-REMOTO", ConexionActiva.Servidor.Hostname, ConexionActiva.Servidor.IP,
                $"Archivo creado: {archivo.Nombre} por {ConexionActiva.Cliente.Hostname}");
            NotificarAccion($"📄 Archivo '{archivo.Nombre}' creado en {ConexionActiva.Servidor.Hostname}");
            return archivo;
        }

        public bool EliminarArchivoEnServidor(SSHArchivo archivo, SSHCarpeta carpetaPadre)
        {
            VerificarConexion();
            bool ok = carpetaPadre.EliminarArchivo(archivo.Nombre);
            if (ok)
            {
                Logger.LogDHCP("SSH-REMOTO", ConexionActiva.Servidor.Hostname, ConexionActiva.Servidor.IP,
                    $"Archivo eliminado: {archivo.Nombre} por {ConexionActiva.Cliente.Hostname}");
                NotificarAccion($"🗑️ Archivo '{archivo.Nombre}' eliminado de {ConexionActiva.Servidor.Hostname}");
            }
            return ok;
        }

        // ── Utilidad ──────────────────────────────────────────────

        private void NotificarAccion(string msg) => AccionRealizada?.Invoke(msg);
    }
}
