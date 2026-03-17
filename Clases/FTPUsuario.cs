using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorRedes.Clases
{
    public class FTPUsuario
    {
        public string Username { get; set; }
        private string Password { get; set; }
        public FTPPermiso Permisos { get; set; }

        public FTPUsuario(string username, string password,
                          FTPPermiso permisos = FTPPermiso.Todos)
        {
            Username = username;
            Password = password;
            Permisos = permisos;
        }

        public bool Autenticar(string contrasena) =>
            string.Equals(Password, contrasena, StringComparison.Ordinal);

        public bool PuedeVer() => Permisos.HasFlag(FTPPermiso.Ver);
        public bool PuedeEditar() => Permisos.HasFlag(FTPPermiso.Editar);
        public bool PuedeEliminar() => Permisos.HasFlag(FTPPermiso.Eliminar);

        public override string ToString() => $"{Username}  [{Permisos}]";
    }
}
