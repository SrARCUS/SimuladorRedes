using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorRedes.Clases
{
    public class FTPManager
    {
        // ── Ruta base en el disco real ────────────────────────────
        public static readonly string RutaBase = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "SimuladorRedes");

        // ── Datos internos ────────────────────────────────────────
        private readonly Dictionary<string, List<FTPUsuario>> _usuarios;

        public FTPConexion ConexionActiva { get; private set; }
        public event Action<string> AccionRealizada;

        public FTPManager()
        {
            _usuarios = new Dictionary<string, List<FTPUsuario>>(
                StringComparer.OrdinalIgnoreCase);
        }

        // ── Inicialización ────────────────────────────────────────
        public void InicializarClientes(IEnumerable<ClienteDHCP> clientes)
        {
            Directory.CreateDirectory(RutaBase);

            var listaClientes = clientes.ToList();

            foreach (var c in listaClientes)
            {
                Directory.CreateDirectory(ObtenerRutaCliente(c.Hostname));

                if (!_usuarios.ContainsKey(c.Hostname))
                {
                    var usuarios = new List<FTPUsuario>();

                    // admin siempre presente con permisos totales
                    usuarios.Add(new FTPUsuario("admin", "admin123", FTPPermiso.Todos));

                    // cada cliente del combobox es un usuario válido de este servidor
                    foreach (var user in listaClientes)
                    {
                        // el servidor no se agrega a sí mismo como usuario
                        if (user.Hostname == c.Hostname) continue;

                        // clientes de la misma organización: Ver + Editar
                        // clientes de otra organización: solo Ver
                        FTPPermiso permiso = user.Organizacion == c.Organizacion
                            ? FTPPermiso.Ver | FTPPermiso.Editar
                            : FTPPermiso.Ver;

                        usuarios.Add(new FTPUsuario(user.Hostname, user.Hostname + "123", permiso));
                    }

                    _usuarios[c.Hostname] = usuarios;
                }
            }
        }

        // ── Rutas ─────────────────────────────────────────────────
        public string ObtenerRutaCliente(string hostname) =>
            Path.Combine(RutaBase, hostname);

        // ── Usuarios ──────────────────────────────────────────────
        public List<FTPUsuario> ObtenerUsuarios(string hostname)
        {
            return _usuarios.TryGetValue(hostname, out var lista)
                   ? lista
                   : new List<FTPUsuario>();
        }

        public void AgregarUsuario(string hostname, FTPUsuario usuario)
        {
            if (!_usuarios.ContainsKey(hostname))
                _usuarios[hostname] = new List<FTPUsuario>();
            _usuarios[hostname].Add(usuario);
        }

        public void EliminarUsuario(string hostname, string username)
        {
            if (!_usuarios.TryGetValue(hostname, out var lista)) return;
            lista.RemoveAll(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public FTPUsuario Autenticar(string hostname, string username, string password)
        {
            if (!_usuarios.TryGetValue(hostname, out var lista))
                throw new Exception($"Servidor '{hostname}' no tiene usuarios configurados.");

            var usuario = lista.FirstOrDefault(
                u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (usuario == null)
                throw new Exception($"Usuario '{username}' no encontrado.");

            if (!usuario.Autenticar(password))
                throw new Exception("Contraseña incorrecta.");

            return usuario;
        }

        // ── Conexión ──────────────────────────────────────────────
        public FTPConexion Conectar(ClienteDHCP cliente, ClienteDHCP servidor,
                                    FTPUsuario usuario)
        {
            if (ConexionActiva != null) Desconectar();

            ConexionActiva = new FTPConexion(cliente, servidor, usuario);
            Logger.Log($"FTP CONNECT: {cliente.Hostname} ({cliente.IP}) → " +
                       $"{servidor.Hostname} ({servidor.IP})  usuario='{usuario.Username}'");
            Notify($"✅ Conectado: {cliente.Hostname} → {servidor.Hostname} [{usuario.Username}]");
            return ConexionActiva;
        }

        public void Desconectar()
        {
            if (ConexionActiva == null) return;
            Logger.Log($"FTP DISCONNECT: {ConexionActiva}  " +
                       $"(duración {(DateTime.Now - ConexionActiva.HoraConexion):mm\\:ss})");
            Notify($"⭕ Desconectado de {ConexionActiva.Servidor.Hostname}");
            ConexionActiva.Activa = false;
            ConexionActiva = null;
        }

        // ── Operaciones de disco ──────────────────────────────────
        public void CrearCarpeta(string rutaCompleta)
        {
            Directory.CreateDirectory(rutaCompleta);
            Logger.Log($"FTP MKDIR: {rutaCompleta}");
            Notify($"📁 Carpeta creada: {Path.GetFileName(rutaCompleta)}");
        }

        public void CrearArchivo(string rutaCompleta, string contenido = "")
        {
            File.WriteAllText(rutaCompleta, contenido);
            Logger.Log($"FTP CREATE: {rutaCompleta}");
            Notify($"📄 Archivo creado: {Path.GetFileName(rutaCompleta)}");
        }

        public void EliminarElemento(string rutaCompleta)
        {
            if (File.Exists(rutaCompleta))
            {
                File.Delete(rutaCompleta);
                Notify($"🗑️ Archivo eliminado: {Path.GetFileName(rutaCompleta)}");
            }
            else if (Directory.Exists(rutaCompleta))
            {
                Directory.Delete(rutaCompleta, recursive: true);
                Notify($"🗑️ Carpeta eliminada: {Path.GetFileName(rutaCompleta)}");
            }
            Logger.Log($"FTP DELETE: {rutaCompleta}");
        }

        /// <summary>Descarga un archivo del servidor remoto al cliente local.</summary>
        public void Transferir(string rutaArchivoRemoto, string hostnameLocal)
        {
            RequiereConexion();

            if (!ConexionActiva.Usuario.PuedeVer())
                throw new UnauthorizedAccessException(
                    "Sin permiso 'Ver' para descargar archivos.");

            if (!File.Exists(rutaArchivoRemoto))
                throw new FileNotFoundException(
                    "El archivo no existe en el servidor.");

            string destino = Path.Combine(
                ObtenerRutaCliente(hostnameLocal),
                Path.GetFileName(rutaArchivoRemoto));

            File.Copy(rutaArchivoRemoto, destino, overwrite: true);
            Logger.Log($"FTP GET: {rutaArchivoRemoto} → {destino}");
            Notify($"📥 Transferido: {Path.GetFileName(rutaArchivoRemoto)} → {hostnameLocal}");
        }
        // Agrega después del método Transferir() existente

        /// <summary>Copia una carpeta completa del servidor remoto al cliente local.</summary>
        public void TransferirCarpeta(string rutaCarpetaRemota, string hostnameLocal)
        {
            RequiereConexion();

            if (!ConexionActiva.Usuario.PuedeVer())
                throw new UnauthorizedAccessException("Sin permiso 'Ver' para descargar carpetas.");

            if (!Directory.Exists(rutaCarpetaRemota))
                throw new DirectoryNotFoundException("La carpeta no existe en el servidor.");

            string destino = Path.Combine(
                ObtenerRutaCliente(hostnameLocal),
                Path.GetFileName(rutaCarpetaRemota));

            CopiarCarpetaRecursiva(rutaCarpetaRemota, destino);

            Logger.Log($"FTP GET DIR: {rutaCarpetaRemota} → {destino}");
            Notify($"📥 Carpeta transferida: {Path.GetFileName(rutaCarpetaRemota)} → {hostnameLocal}");
        }

        /// <summary>Sube una carpeta completa del cliente local al servidor remoto.</summary>
        public void EnviarCarpeta(string rutaCarpetaLocal, string hostnameServidor)
        {
            RequiereConexion();

            if (!ConexionActiva.Usuario.PuedeEditar())
                throw new UnauthorizedAccessException("Sin permiso 'Editar' para subir carpetas.");

            if (!Directory.Exists(rutaCarpetaLocal))
                throw new DirectoryNotFoundException("La carpeta local no existe.");

            string destino = Path.Combine(
                ObtenerRutaCliente(hostnameServidor),
                Path.GetFileName(rutaCarpetaLocal));

            CopiarCarpetaRecursiva(rutaCarpetaLocal, destino);

            Logger.Log($"FTP PUT DIR: {rutaCarpetaLocal} → {destino}");
            Notify($"📤 Carpeta enviada: {Path.GetFileName(rutaCarpetaLocal)} → {hostnameServidor}");
        }

        /// <summary>Copia recursivamente una carpeta y todo su contenido.</summary>
        private static void CopiarCarpetaRecursiva(string origen, string destino)
        {
            Directory.CreateDirectory(destino);

            // Copiar archivos del nivel actual
            foreach (string archivo in Directory.GetFiles(origen))
            {
                string archivoDestino = Path.Combine(destino, Path.GetFileName(archivo));
                File.Copy(archivo, archivoDestino, overwrite: true);
            }

            // Copiar subcarpetas recursivamente
            foreach (string subcarpeta in Directory.GetDirectories(origen))
            {
                string subcarpetaDestino = Path.Combine(destino, Path.GetFileName(subcarpeta));
                CopiarCarpetaRecursiva(subcarpeta, subcarpetaDestino);
            }
        }
        /// <summary>Sube un archivo local al servidor remoto.</summary>
        public void Enviar(string rutaArchivoLocal, string hostnameServidor)
        {
            RequiereConexion();

            if (!ConexionActiva.Usuario.PuedeEditar())
                throw new UnauthorizedAccessException(
                    "Sin permiso 'Editar' para subir archivos.");

            if (!File.Exists(rutaArchivoLocal))
                throw new FileNotFoundException("El archivo local no existe.");

            string destino = Path.Combine(
                ObtenerRutaCliente(hostnameServidor),
                Path.GetFileName(rutaArchivoLocal));

            File.Copy(rutaArchivoLocal, destino, overwrite: true);
            Logger.Log($"FTP PUT: {rutaArchivoLocal} → {destino}");
            Notify($"📤 Enviado: {Path.GetFileName(rutaArchivoLocal)} → {hostnameServidor}");
        }

        // ── Internos ──────────────────────────────────────────────
        private void RequiereConexion()
        {
            if (ConexionActiva == null)
                throw new InvalidOperationException("No hay una conexión FTP activa.");
        }

        private void Notify(string msg) => AccionRealizada?.Invoke(msg);
    }
}
