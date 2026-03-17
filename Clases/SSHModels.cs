using System;
using System.Collections.Generic;
using System.Linq;

namespace SimuladorRedes
{
    // ─────────────────────────────────────────────────────────────
    //  Representa un archivo de texto dentro del sistema simulado
    // ─────────────────────────────────────────────────────────────
    public class SSHArchivo
    {
        public string Nombre { get; set; }
        public string Contenido { get; set; }
        public DateTime FechaCreacion { get; private set; }
        public DateTime FechaModificacion { get; set; }

        public SSHArchivo(string nombre, string contenido = "")
        {
            // Garantizar extensión .txt
            Nombre = nombre.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                     ? nombre : nombre + ".txt";
            Contenido = contenido;
            FechaCreacion = DateTime.Now;
            FechaModificacion = DateTime.Now;
        }

        public override string ToString() => Nombre;
    }

    // ─────────────────────────────────────────────────────────────
    //  Representa una carpeta que puede contener archivos .txt
    //  y sub-carpetas (árbol recursivo)
    // ─────────────────────────────────────────────────────────────
    public class SSHCarpeta
    {
        public string Nombre { get; set; }
        public List<SSHArchivo> Archivos { get; private set; }
        public List<SSHCarpeta> Subcarpetas { get; private set; }
        public SSHCarpeta Padre { get; set; }

        public SSHCarpeta(string nombre)
        {
            Nombre = nombre;
            Archivos = new List<SSHArchivo>();
            Subcarpetas = new List<SSHCarpeta>();
            Padre = null;
        }

        // ── Archivos ──────────────────────────────────────────────

        /// <summary>Crea un archivo .txt en esta carpeta. Devuelve null si ya existe.</summary>
        public SSHArchivo CrearArchivo(string nombre, string contenido = "")
        {
            string nombreFinal = nombre.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                                 ? nombre : nombre + ".txt";

            if (Archivos.Any(a => a.Nombre.Equals(nombreFinal, StringComparison.OrdinalIgnoreCase)))
                return null;

            var archivo = new SSHArchivo(nombreFinal, contenido);
            Archivos.Add(archivo);
            return archivo;
        }

        /// <summary>Elimina un archivo por nombre. Devuelve true si tuvo éxito.</summary>
        public bool EliminarArchivo(string nombre)
        {
            var archivo = Archivos.FirstOrDefault(
                a => a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

            if (archivo != null)
            {
                Archivos.Remove(archivo);
                return true;
            }
            return false;
        }

        // ── Subcarpetas ───────────────────────────────────────────

        /// <summary>Crea una sub-carpeta. Devuelve null si ya existe.</summary>
        public SSHCarpeta CrearSubcarpeta(string nombre)
        {
            if (Subcarpetas.Any(c => c.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase)))
                return null;

            var carpeta = new SSHCarpeta(nombre) { Padre = this };
            Subcarpetas.Add(carpeta);
            return carpeta;
        }

        /// <summary>Elimina una sub-carpeta por nombre (con todo su contenido).</summary>
        public bool EliminarSubcarpeta(string nombre)
        {
            var carpeta = Subcarpetas.FirstOrDefault(
                c => c.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));

            if (carpeta != null)
            {
                carpeta.Padre = null;
                Subcarpetas.Remove(carpeta);
                return true;
            }
            return false;
        }

        // ── Utilidades ────────────────────────────────────────────

        /// <summary>Ruta completa desde la raíz, e.g. /hostname/docs/trabajo</summary>
        public string ObtenerRuta()
        {
            return Padre == null ? "/" + Nombre : Padre.ObtenerRuta() + "/" + Nombre;
        }

        public bool EsRaiz() => Padre == null;

        public int ContarTotalArchivos()
        {
            int total = Archivos.Count;
            foreach (var sub in Subcarpetas)
                total += sub.ContarTotalArchivos();
            return total;
        }

        public override string ToString() => Nombre;
    }
}
