using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public static class Logger
    {
        private static ListBox listBoxLog;
        private static string archivoLog = "simulador_log.txt";
        private static bool inicializado = false;

        public static void Inicializar(ListBox listBox)
        {
            listBoxLog = listBox;
            inicializado = true;
            Log("Sistema de logging inicializado");
        }

        public static void Log(string mensaje)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string entradaLog = $"[{timestamp}] {mensaje}";

            // Mostrar en el ListBox si está disponible
            if (inicializado && listBoxLog != null && !listBoxLog.IsDisposed)
            {
                if (listBoxLog.InvokeRequired)
                {
                    listBoxLog.Invoke(new Action(() =>
                    {
                        listBoxLog.Items.Add(entradaLog);
                        listBoxLog.TopIndex = listBoxLog.Items.Count - 1; // Auto-scroll
                    }));
                }
                else
                {
                    listBoxLog.Items.Add(entradaLog);
                    listBoxLog.TopIndex = listBoxLog.Items.Count - 1;
                }
            }

            // Escribir en archivo
            try
            {
                File.AppendAllText(archivoLog, entradaLog + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Si no se puede escribir en archivo, al menos mostrar en consola
                Console.WriteLine($"Error escribiendo log: {ex.Message}");
            }
        }

        public static void LogDHCP(string accion, string cliente, string ip, string detalles = "")
        {
            string mensaje = $"DHCP - {accion}: {cliente} [{ip}]";
            if (!string.IsNullOrEmpty(detalles))
                mensaje += $" - {detalles}";
            Log(mensaje);
        }

        public static void LogDNS(string accion, string dispositivo, string ip, string dominio, string detalles = "")
        {
            string mensaje = $"DNS - {accion}: {dispositivo}.{dominio} [{ip}]";
            if (!string.IsNullOrEmpty(detalles))
                mensaje += $" - {detalles}";
            Log(mensaje);
        }

        public static void LogEvento(string tipo, string mensaje)
        {
            Log($"{tipo}: {mensaje}");
        }

        public static void LimpiarLog()
        {
            if (inicializado && listBoxLog != null && !listBoxLog.IsDisposed)
            {
                if (listBoxLog.InvokeRequired)
                {
                    listBoxLog.Invoke(new Action(() => listBoxLog.Items.Clear()));
                }
                else
                {
                    listBoxLog.Items.Clear();
                }
            }

            try
            {
                File.WriteAllText(archivoLog, $"--- Log iniciado {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---\n");
            }
            catch { }

            Log("Log limpiado");
        }

        public static void ExportarLog()
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Archivos de texto|*.txt|Todos los archivos|*.*";
            saveDialog.Title = "Guardar archivo de log";
            saveDialog.FileName = $"simulador_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.Copy(archivoLog, saveDialog.FileName, true);
                    Log($"Log exportado a: {saveDialog.FileName}");
                    MessageBox.Show($"Log exportado exitosamente a:\n{saveDialog.FileName}",
                        "Exportación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar log: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}