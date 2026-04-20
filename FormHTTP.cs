using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public partial class FormHTTP : Form
    {
        // ── Controles principales ─────────────────────────────────
        private RichTextBox txtEditor;
        private WebBrowser webPreview;
        private Button btnActualizar;
        private Button btnGuardar;
        private Button btnAbrirNavegador;
        private Button btnNuevo;
        private Button btnAbrir;
        private CheckBox chkAutoPreview;
        private Label lblRutaActual;
        private Label lblEstado;
        private SplitContainer splitMain;

        // ── Estado ────────────────────────────────────────────────
        private string rutaArchivoActual = null;
        private bool cambiosSinGuardar = false;

        // ── Ruta base (misma lógica que FTPManager) ───────────────
        public static readonly string RutaBase = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "SimuladorRedes", "HTTP");

        // ── Plantilla HTML completa ───────────────────────────────
        private const string PlantillaHTML =
@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Mi Página Web - SimuladorRedes</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: 'Segoe UI', Arial, sans-serif; background: #f0f4f8; color: #333; }

        /* ── Barra de navegación ── */
        nav {
            background: #1a3a5c;
            padding: 14px 24px;
            display: flex;
            align-items: center;
            gap: 24px;
        }
        nav .logo { color: #fff; font-size: 20px; font-weight: bold; letter-spacing: 1px; }
        nav a { color: #a8d0f0; text-decoration: none; font-size: 14px; }
        nav a:hover { color: #fff; text-decoration: underline; }

        /* ── Héroe ── */
        .hero {
            background: linear-gradient(135deg, #1a3a5c, #2e7bcf);
            color: #fff;
            text-align: center;
            padding: 60px 20px;
        }
        .hero h1 { font-size: 36px; margin-bottom: 12px; }
        .hero p  { font-size: 16px; opacity: .85; }

        /* ── Contenido principal ── */
        main { max-width: 900px; margin: 32px auto; padding: 0 16px; }

        /* ── Secciones ── */
        section { background: #fff; border-radius: 8px; padding: 24px; margin-bottom: 24px;
                  box-shadow: 0 2px 6px rgba(0,0,0,.08); }
        section h2 { color: #1a3a5c; margin-bottom: 12px; border-bottom: 2px solid #e0e8f0; padding-bottom: 8px; }
        section p  { line-height: 1.7; margin-bottom: 10px; }
        ul { margin-left: 20px; line-height: 1.8; }

        /* ── Tabla ── */
        table { width: 100%; border-collapse: collapse; margin-top: 12px; }
        th { background: #1a3a5c; color: #fff; padding: 10px 14px; text-align: left; }
        td { padding: 9px 14px; border-bottom: 1px solid #e0e8f0; }
        tr:hover td { background: #f5f9ff; }

        /* ── Formulario ── */
        form { display: flex; flex-direction: column; gap: 14px; }
        label { font-weight: 600; font-size: 14px; }
        input, textarea, select {
            padding: 9px 12px; border: 1px solid #cdd6e0; border-radius: 5px;
            font-size: 14px; width: 100%;
        }
        input:focus, textarea:focus { outline: none; border-color: #2e7bcf; box-shadow: 0 0 0 3px rgba(46,123,207,.15); }

        /* ── Botones ── */
        .btn {
            display: inline-block; padding: 11px 28px; border: none; border-radius: 6px;
            font-size: 15px; font-weight: bold; cursor: pointer; transition: opacity .2s;
        }
        .btn-primary { background: #2e7bcf; color: #fff; }
        .btn-success { background: #28a745; color: #fff; }
        .btn-warning { background: #f0a500; color: #fff; }
        .btn:hover { opacity: .85; }

        /* ── Pie de página ── */
        footer { text-align: center; padding: 20px; color: #888; font-size: 13px;
                 border-top: 1px solid #dde4ec; margin-top: 16px; }
    </style>
</head>
<body>

<!-- ══ NAVEGACIÓN ══ -->
<nav>
    <span class=""logo"">🌐 SimuladorRedes</span>
    <a href=""#inicio"">Inicio</a>
    <a href=""#protocolos"">Protocolos</a>
    <a href=""#tabla"">Tabla</a>
    <a href=""#contacto"">Contacto</a>
</nav>

<!-- ══ HÉROE ══ -->
<div class=""hero"" id=""inicio"">
    <h1>Bienvenido al Simulador de Redes</h1>
    <p>Visualiza y experimenta con protocolos: DHCP · DNS · SSH · FTP · HTTP</p>
</div>

<main>

    <!-- ══ SECCIÓN: INFORMACIÓN ══ -->
    <section id=""protocolos"">
        <h2>📡 Protocolos de Red</h2>
        <p>
            Este simulador te permite explorar los principales protocolos de la capa de
            aplicación. Cada módulo muestra el comportamiento real de los servicios de red.
        </p>
        <ul>
            <li><strong>DHCP</strong> — Asignación dinámica de direcciones IP</li>
            <li><strong>DNS</strong>  — Resolución de nombres de dominio</li>
            <li><strong>SSH</strong>  — Acceso remoto seguro y gestión de archivos</li>
            <li><strong>FTP</strong>  — Transferencia de archivos entre clientes y servidores</li>
            <li><strong>HTTP</strong> — Protocolo de transferencia de hipertexto (¡esta página!)</li>
        </ul>
    </section>

    <!-- ══ SECCIÓN: TABLA ══ -->
    <section id=""tabla"">
        <h2>📋 Resumen de Clientes</h2>
        <table>
            <thead>
                <tr>
                    <th>Hostname</th>
                    <th>IP</th>
                    <th>Dominio</th>
                    <th>Estado</th>
                </tr>
            </thead>
            <tbody>
                <tr><td>google</td>     <td>10.0.0.1</td>      <td>google.com</td>  <td>✅ Activo</td></tr>
                <tr><td>marketing</td>  <td>10.0.1.1</td>      <td>google.com</td>  <td>✅ Activo</td></tr>
                <tr><td>pc-ana</td>     <td>10.0.1.10</td>     <td>google.com</td>  <td>✅ Activo</td></tr>
                <tr><td>tecnm</td>      <td>192.168.1.1</td>   <td>tecnm.mx</td>    <td>✅ Activo</td></tr>
                <tr><td>isc</td>        <td>192.168.2.1</td>   <td>tecnm.mx</td>    <td>✅ Activo</td></tr>
                <tr><td>server-isc</td> <td>192.168.2.10</td>  <td>tecnm.mx</td>    <td>✅ Activo</td></tr>
            </tbody>
        </table>
    </section>

    <!-- ══ SECCIÓN: FORMULARIO ══ -->
    <section id=""contacto"">
        <h2>📬 Formulario de Contacto</h2>
        <form onsubmit=""return enviarFormulario(event)"">
            <div>
                <label for=""nombre"">Nombre:</label>
                <input type=""text"" id=""nombre"" placeholder=""Tu nombre completo"">
            </div>
            <div>
                <label for=""email"">Correo electrónico:</label>
                <input type=""email"" id=""email"" placeholder=""correo@ejemplo.com"">
            </div>
            <div>
                <label for=""asunto"">Asunto:</label>
                <select id=""asunto"">
                    <option>Consulta general</option>
                    <option>Soporte técnico</option>
                    <option>Reporte de error</option>
                </select>
            </div>
            <div>
                <label for=""mensaje"">Mensaje:</label>
                <textarea id=""mensaje"" rows=""4"" placeholder=""Escribe tu mensaje aquí...""></textarea>
            </div>
            <div style=""display:flex; gap:12px; flex-wrap:wrap;"">
                <button type=""submit"" class=""btn btn-primary"">📨 Enviar mensaje</button>
                <button type=""button"" class=""btn btn-warning"" onclick=""saludar()"">👋 Hola Mundo</button>
                <button type=""button"" class=""btn btn-success"" onclick=""mostrarInfo()"">ℹ️ Info del simulador</button>
            </div>
        </form>
    </section>

</main>

<footer>
    <p>Simulador de Redes — HTTP &nbsp;·&nbsp; .NET Framework 4.8 &nbsp;·&nbsp; 2025</p>
</footer>

<script>
    // ── Hola Mundo ──
    function saludar() {
        alert('👋 ¡Hola Mundo!\nEste mensaje fue generado por un botón HTML\nen el Simulador de Redes.');
    }

    // ── Info del simulador ──
    function mostrarInfo() {
        alert(
            '🌐 Simulador de Redes\n' +
            '──────────────────────\n' +
            'Módulos activos:\n' +
            '  • DHCP  – asignación de IPs\n' +
            '  • DNS   – resolución de nombres\n' +
            '  • SSH   – acceso remoto\n' +
            '  • FTP   – transferencia de archivos\n' +
            '  • HTTP  – visualización de páginas\n' +
            '──────────────────────\n' +
            'Versión 1.0 — C# / WinForms'
        );
    }

    // ── Formulario ──
    function enviarFormulario(e) {
        e.preventDefault();
        var nombre  = document.getElementById('nombre').value.trim();
        var email   = document.getElementById('email').value.trim();
        var asunto  = document.getElementById('asunto').value;
        var mensaje = document.getElementById('mensaje').value.trim();

        if (!nombre || !email || !mensaje) {
            alert('⚠️ Por favor completa todos los campos antes de enviar.');
            return false;
        }

        alert(
            '✅ Formulario enviado correctamente\n' +
            '──────────────────────\n' +
            'Nombre:  ' + nombre  + '\n' +
            'Email:   ' + email   + '\n' +
            'Asunto:  ' + asunto  + '\n' +
            'Mensaje: ' + mensaje
        );

        e.target.reset();
        return false;
    }
</script>

</body>
</html>";

        // ─────────────────────────────────────────────────────────
        public FormHTTP()
        {
            InitializeComponent();
            InicializarUI();
            Directory.CreateDirectory(RutaBase);
            CargarPlantilla();
        }

        // ═════════════════════════════════════════════════════════
        //  Construcción de la interfaz
        // ═════════════════════════════════════════════════════════
        private void InicializarUI()
        {
            this.Text = "Simulador de Redes — HTTP";
            this.Size = new Size(1300, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 680);

            // ── 1. Barra de herramientas superior ─────────────────
            Panel panelTools = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1300, 52),
                BackColor = Color.FromArgb(20, 60, 120),
                Dock = DockStyle.Top
            };

            // Título
            Label lblTitulo = new Label
            {
                Text = "🌐  HTTP  —  Editor de páginas web",
                Location = new Point(14, 14),
                Size = new Size(300, 24),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.Gold,
                BackColor = Color.Transparent
            };

            // Botones de la barra
            btnNuevo = MkToolBtn("📄 Nuevo", new Point(310, 10), Color.FromArgb(70, 130, 200));
            btnAbrir = MkToolBtn("📂 Abrir", new Point(415, 10), Color.FromArgb(70, 130, 200));
            btnGuardar = MkToolBtn("💾 Guardar", new Point(520, 10), Color.FromArgb(30, 140, 80));
            btnActualizar = MkToolBtn("🔄 Vista Previa", new Point(635, 10), Color.FromArgb(190, 130, 0));
            btnAbrirNavegador = MkToolBtn("🌍 Abrir en Navegador", new Point(760, 10), Color.FromArgb(140, 60, 160));
            btnAbrirNavegador.Size = new Size(170, 32);

            chkAutoPreview = new CheckBox
            {
                Text = "Auto-preview",
                Location = new Point(945, 16),
                Size = new Size(110, 22),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9),
                Checked = false
            };

            panelTools.Controls.AddRange(new Control[] {
                lblTitulo, btnNuevo, btnAbrir, btnGuardar,
                btnActualizar, btnAbrirNavegador, chkAutoPreview
            });

            // ── 2. Barra de estado inferior ────────────────────────
            Panel panelStatus = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(230, 235, 242)
            };

            lblRutaActual = new Label
            {
                Text = "  Sin guardar",
                Location = new Point(4, 4),
                Size = new Size(700, 18),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DarkSlateGray
            };

            lblEstado = new Label
            {
                Text = "Listo",
                Location = new Point(1100, 4),
                Size = new Size(180, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                TextAlign = ContentAlignment.MiddleRight
            };

            panelStatus.Controls.AddRange(new Control[] { lblRutaActual, lblEstado });

            // ── 3. SplitContainer (editor | preview) ─────────────
            splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 480,
                SplitterWidth = 5,
                BackColor = Color.FromArgb(200, 210, 220)
            };

            // Panel izquierdo — Editor ─────────────────────────────
            GroupBox grpEditor = new GroupBox
            {
                Text = "  ✏️  Editor HTML",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 60, 120)
            };

            txtEditor = new RichTextBox
            {
               
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(18, 18, 30),
                ForeColor = Color.FromArgb(220, 230, 250),
                BorderStyle = BorderStyle.None,
                AcceptsTab = true
            };
            txtEditor.TextChanged += TxtEditor_TextChanged;

            grpEditor.Controls.Add(txtEditor);
            splitMain.Panel1.Controls.Add(grpEditor);

            // Panel derecho — Vista previa ─────────────────────────
            GroupBox grpPreview = new GroupBox
            {
                Text = "  🌐  Vista Previa",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 60, 120)
            };

            webPreview = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true,
                WebBrowserShortcutsEnabled = false,
                IsWebBrowserContextMenuEnabled = true
            };

            grpPreview.Controls.Add(webPreview);
            splitMain.Panel2.Controls.Add(grpPreview);

            // ── Conectar eventos de botones ───────────────────────
            btnNuevo.Click += BtnNuevo_Click;
            btnAbrir.Click += BtnAbrir_Click;
            btnGuardar.Click += BtnGuardar_Click;
            btnActualizar.Click += (s, e) => ActualizarPreview();
            btnAbrirNavegador.Click += BtnAbrirNavegador_Click;

            // ── Añadir al formulario ──────────────────────────────
            this.Controls.Add(splitMain);
            this.Controls.Add(panelTools);
            this.Controls.Add(panelStatus);
        }

        // ═════════════════════════════════════════════════════════
        //  Lógica
        // ═════════════════════════════════════════════════════════
        private void CargarPlantilla()
        {
            txtEditor.Text = PlantillaHTML;
            cambiosSinGuardar = false;
            ActualizarPreview();
            SetEstado("Plantilla cargada", Color.DarkGreen);
        }

        private void ActualizarPreview()
        {
            try
            {
                string html = txtEditor.Text;

                // Escribir a un archivo temporal para que el WebBrowser
                // pueda ejecutar JavaScript correctamente
                string tmpPath = Path.Combine(Path.GetTempPath(), "SimHTTP_preview.html");
                File.WriteAllText(tmpPath, html, System.Text.Encoding.UTF8);
                webPreview.Navigate(new Uri(tmpPath));

                SetEstado("Vista previa actualizada", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                SetEstado($"Error en preview: {ex.Message}", Color.DarkRed);
            }
        }

        private void TxtEditor_TextChanged(object sender, EventArgs e)
        {
            cambiosSinGuardar = true;
            ActualizarTituloVentana();

            if (chkAutoPreview.Checked)
                ActualizarPreview();
        }

        // ── Nuevo ─────────────────────────────────────────────────
        private void BtnNuevo_Click(object sender, EventArgs e)
        {
            if (cambiosSinGuardar)
            {
                var r = MessageBox.Show("¿Descartar los cambios actuales y cargar la plantilla?",
                    "Nuevo archivo", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            rutaArchivoActual = null;
            CargarPlantilla();
            ActualizarTituloVentana();
            lblRutaActual.Text = "  Sin guardar";
        }

        // ── Abrir ─────────────────────────────────────────────────
        private void BtnAbrir_Click(object sender, EventArgs e)
        {
            if (cambiosSinGuardar)
            {
                var r = MessageBox.Show("¿Descartar los cambios sin guardar?",
                    "Abrir archivo", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return;
            }

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Abrir archivo HTML";
                dlg.Filter = "Archivos HTML|*.html;*.htm|Todos los archivos|*.*";
                dlg.InitialDirectory = Directory.Exists(RutaBase) ? RutaBase
                    : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        txtEditor.Text = File.ReadAllText(dlg.FileName, System.Text.Encoding.UTF8);
                        rutaArchivoActual = dlg.FileName;
                        cambiosSinGuardar = false;
                        ActualizarPreview();
                        ActualizarTituloVentana();
                        lblRutaActual.Text = $"  {rutaArchivoActual}";
                        SetEstado("Archivo abierto", Color.DarkGreen);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir el archivo:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ── Guardar ───────────────────────────────────────────────
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rutaArchivoActual))
                GuardarComo();
            else
                GuardarEnRuta(rutaArchivoActual);
        }

        private void GuardarComo()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Guardar página HTML";
                dlg.Filter = "Página HTML|*.html|Todos los archivos|*.*";
                dlg.DefaultExt = "html";
                dlg.InitialDirectory = RutaBase;
                dlg.FileName = "pagina.html";

                if (dlg.ShowDialog() == DialogResult.OK)
                    GuardarEnRuta(dlg.FileName);
            }
        }

        private void GuardarEnRuta(string ruta)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ruta));
                File.WriteAllText(ruta, txtEditor.Text, System.Text.Encoding.UTF8);
                rutaArchivoActual = ruta;
                cambiosSinGuardar = false;
                ActualizarTituloVentana();
                lblRutaActual.Text = $"  {rutaArchivoActual}";
                SetEstado("Guardado correctamente ✔", Color.DarkGreen);
                Logger.Log($"HTTP - Archivo guardado: {ruta}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Abrir en navegador del sistema ────────────────────────
        private void BtnAbrirNavegador_Click(object sender, EventArgs e)
        {
            // Si no tiene ruta, guardar primero en la carpeta HTTP
            string ruta = rutaArchivoActual;

            if (string.IsNullOrEmpty(ruta))
            {
                ruta = Path.Combine(RutaBase, "pagina.html");
                try
                {
                    Directory.CreateDirectory(RutaBase);
                    File.WriteAllText(ruta, txtEditor.Text, System.Text.Encoding.UTF8);
                    rutaArchivoActual = ruta;
                    cambiosSinGuardar = false;
                    ActualizarTituloVentana();
                    lblRutaActual.Text = $"  {ruta}";
                    Logger.Log($"HTTP - Guardado automático para navegador: {ruta}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo guardar el archivo:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else if (cambiosSinGuardar)
            {
                // Guardar cambios pendientes antes de abrir
                GuardarEnRuta(ruta);
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
                SetEstado("Abierto en navegador externo", Color.DarkBlue);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir el navegador:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Utilidades ────────────────────────────────────────────
        private void ActualizarTituloVentana()
        {
            string nombre = string.IsNullOrEmpty(rutaArchivoActual)
                ? "Sin título"
                : Path.GetFileName(rutaArchivoActual);

            string asterisco = cambiosSinGuardar ? "* " : "";
            this.Text = $"{asterisco}{nombre} — HTTP Editor · SimuladorRedes";
        }

        private void SetEstado(string texto, Color color)
        {
            lblEstado.Text = texto;
            lblEstado.ForeColor = color;
        }

        private Button MkToolBtn(string texto, Point ubicacion, Color colorFondo)
        {
            var btn = new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(100, 32),
                BackColor = colorFondo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (cambiosSinGuardar)
            {
                var r = MessageBox.Show("Hay cambios sin guardar. ¿Deseas guardar antes de cerrar?",
                    "Cerrar", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (r == DialogResult.Yes)
                    BtnGuardar_Click(null, null);
                else if (r == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Limpiar archivo temporal de preview
            try
            {
                string tmp = Path.Combine(Path.GetTempPath(), "SimHTTP_preview.html");
                if (File.Exists(tmp)) File.Delete(tmp);
            }
            catch { }

            base.OnFormClosing(e);
        }
    }
}
