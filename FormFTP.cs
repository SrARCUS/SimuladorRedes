using SimuladorRedes.Clases;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public partial class FormFTP : Form
    {
        private readonly FTPManager ftpManager;
        private readonly DHCPManager dhcpManager;

        // ── Conexión ──────────────────────────────────────────────
        private ComboBox cmbClienteLocal;
        private ComboBox cmbServidor;
        private Button btnConectar;
        private Button btnDesconectar;
        private Label lblEstado;

        // ── Árboles ───────────────────────────────────────────────
        private TreeView treeRemoto;
        private TreeView treeLocal;
        private Label lblInfoRemoto;
        private Label lblInfoLocal;

        // ── Botones centrales ─────────────────────────────────────
        private Button btnTransferir;
        private Button btnEnviar;
        private Label lblConectadoComo;

        // ── Botones remoto ────────────────────────────────────────
        private Button btnCrearCarpetaRemoto;
        private Button btnCrearArchivoRemoto;
        private Button btnEliminarRemoto;

        // ── Botones local ─────────────────────────────────────────
        private Button btnCrearCarpetaLocal;
        private Button btnCrearArchivoLocal;
        private Button btnEliminarLocal;

        // ── Permisos ──────────────────────────────────────────────
        private GroupBox groupPermisos;
        private Label lblNombreUsuario;
        private CheckBox chkVer;
        private CheckBox chkEditar;
        private CheckBox chkEliminar;
        private CheckBox chkTodos;

        // ── Log ───────────────────────────────────────────────────
        private ListBox lstLog;

        // ─────────────────────────────────────────────────────────
        // ── Editor de texto ───────────────────────────────────────────
        private GroupBox groupEditor;
        private TextBox txtContenido;
        private Button btnGuardarTxt;
        private Label lblArchivoEdit;
        private string rutaArchivoEditando;
        public FormFTP(DHCPManager manager)
        {
            dhcpManager = manager;
            ftpManager = new FTPManager();
            ftpManager.InicializarClientes(dhcpManager.Clientes);
            ftpManager.AccionRealizada += msg => SafeInvoke(() => RegistrarLog(msg));

            InicializarUI();
            CargarCombos();
        }

        // ═════════════════════════════════════════════════════════
        //  Construcción de la interfaz
        // ═════════════════════════════════════════════════════════
        private void InicializarUI()
        {
            this.Text = "Simulador de Redes — FTP";
            this.Size = new Size(1200, 740);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 680);

            // ── 1. Barra de conexión ──────────────────────────────
            Panel panelCon = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(1160, 56),
                BackColor = Color.FromArgb(25, 45, 90)
            };

            AddLabel(panelCon, "📂  FTP", new Point(10, 10),
                     "Segoe UI", 14, FontStyle.Bold, Color.Gold);
            AddLabel(panelCon, "Local:", new Point(88, 18),
                     "Segoe UI", 9, FontStyle.Regular, Color.White);

            cmbClienteLocal = new ComboBox
            {
                Location = new Point(142, 14),
                Size = new Size(195, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbClienteLocal.SelectedIndexChanged += (s, e) => CargarArbolLocal();

            AddLabel(panelCon, "──FTP──▶", new Point(345, 18),
                     "Consolas", 9, FontStyle.Bold, Color.Gold);
            AddLabel(panelCon, "Servidor:", new Point(435, 18),
                     "Segoe UI", 9, FontStyle.Regular, Color.White);

            cmbServidor = new ComboBox
            {
                Location = new Point(508, 14),
                Size = new Size(205, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };

            btnConectar = MkBtn(panelCon, "🔗  Conectar",
                new Point(724, 12), new Size(120, 32),
                Color.FromArgb(0, 165, 85));
            btnConectar.Click += BtnConectar_Click;

            btnDesconectar = MkBtn(panelCon, "✂  Desconectar",
                new Point(854, 12), new Size(135, 32),
                Color.FromArgb(190, 50, 50));
            btnDesconectar.Enabled = false;
            btnDesconectar.Click += BtnDesconectar_Click;

            lblEstado = new Label
            {
                Text = "⭕  Sin conexión",
                Location = new Point(1000, 18),
                Size = new Size(155, 22),
                ForeColor = Color.Tomato,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };

            panelCon.Controls.AddRange(
                new Control[] { cmbClienteLocal, cmbServidor, lblEstado });

            // ── 2. Panel REMOTO ───────────────────────────────────
            GroupBox grpRemoto = new GroupBox
            {
                Text = "🖧  PC Remota",
                Location = new Point(12, 78),
                Size = new Size(390, 490),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblInfoRemoto = new Label
            {
                Text = "Sin conexión FTP",
                Location = new Point(6, 20),
                Size = new Size(378, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            treeRemoto = new TreeView
            {
                Location = new Point(6, 42),
                Size = new Size(378, 366),
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(255, 248, 240),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };

            btnCrearCarpetaRemoto = MkBtn(grpRemoto, "📁 Carpeta",
                new Point(6, 418), new Size(115, 28), Color.FromArgb(255, 210, 55));
            btnCrearArchivoRemoto = MkBtn(grpRemoto, "📄 Archivo",
                new Point(127, 418), new Size(115, 28), Color.FromArgb(80, 185, 255));
            btnEliminarRemoto = MkBtn(grpRemoto, "🗑 Eliminar",
                new Point(248, 418), new Size(115, 28), Color.FromArgb(255, 100, 90));

            btnCrearCarpetaRemoto.Enabled =
            btnCrearArchivoRemoto.Enabled =
            btnEliminarRemoto.Enabled = false;

            btnCrearCarpetaRemoto.Click += BtnCrearCarpetaRemoto_Click;
            btnCrearArchivoRemoto.Click += BtnCrearArchivoRemoto_Click;
            btnEliminarRemoto.Click += BtnEliminarRemoto_Click;

            grpRemoto.Controls.AddRange(new Control[]
            {
                lblInfoRemoto, treeRemoto,
                btnCrearCarpetaRemoto, btnCrearArchivoRemoto, btnEliminarRemoto
            });

            // ── 3. Panel CENTRAL ──────────────────────────────────
            Panel panelCentro = new Panel
            {
                Location = new Point(410, 78),
                Size = new Size(148, 490)
            };

            lblConectadoComo = new Label
            {
                Text = "Sin conexión",
                Location = new Point(4, 12),
                Size = new Size(140, 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            btnTransferir = MkBtn(panelCentro, "📥 Transferir\nRemoto → Local",
                new Point(14, 68), new Size(120, 58),
                Color.FromArgb(50, 120, 220));
            btnTransferir.ForeColor = Color.White;

            btnEnviar = MkBtn(panelCentro, "📤 Enviar\nLocal → Remoto",
                new Point(14, 138), new Size(120, 58),
                Color.FromArgb(40, 175, 90));
            btnEnviar.ForeColor = Color.White;

            btnTransferir.Enabled = false;
            btnEnviar.Enabled = false;
            btnTransferir.Click += BtnTransferir_Click;
            btnEnviar.Click += BtnEnviar_Click;

            panelCentro.Controls.AddRange(
                new Control[] { lblConectadoComo, btnTransferir, btnEnviar });

            // ── 4. Panel LOCAL ────────────────────────────────────
            GroupBox grpLocal = new GroupBox
            {
                Text = "📁  Mi PC  (local)",
                Location = new Point(566, 78),
                Size = new Size(390, 490),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblInfoLocal = new Label
            {
                Text = "Selecciona un cliente",
                Location = new Point(6, 20),
                Size = new Size(378, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            treeLocal = new TreeView
            {
                Location = new Point(6, 42),
                Size = new Size(378, 366),
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(243, 252, 243),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnCrearCarpetaLocal = MkBtn(grpLocal, "📁 Carpeta",
                new Point(6, 418), new Size(115, 28), Color.FromArgb(255, 210, 55));
            btnCrearArchivoLocal = MkBtn(grpLocal, "📄 Archivo",
                new Point(127, 418), new Size(115, 28), Color.FromArgb(80, 185, 255));
            btnEliminarLocal = MkBtn(grpLocal, "🗑 Eliminar",
                new Point(248, 418), new Size(115, 28), Color.FromArgb(255, 100, 90));

            btnCrearCarpetaLocal.Click += BtnCrearCarpetaLocal_Click;
            btnCrearArchivoLocal.Click += BtnCrearArchivoLocal_Click;
            btnEliminarLocal.Click += BtnEliminarLocal_Click;

            grpLocal.Controls.AddRange(new Control[]
            {
                lblInfoLocal, treeLocal,
                btnCrearCarpetaLocal, btnCrearArchivoLocal, btnEliminarLocal
            });

            // ── 5. Panel PERMISOS ─────────────────────────────────
            groupPermisos = new GroupBox
            {
                Text = "🔑  Permisos",
                Location = new Point(966, 78),
                Size = new Size(218, 270),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            new Label
            {
                Text = "Permisos para:",
                Location = new Point(8, 22),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 8),
                Parent = groupPermisos
            };

            lblNombreUsuario = new Label
            {
                Text = "—",
                Location = new Point(8, 42),
                Size = new Size(200, 22),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            chkVer = MkCheck("🔍  Ver", new Point(16, 72));
            chkEditar = MkCheck("✏️  Editar", new Point(16, 104));
            chkEliminar = MkCheck("🗑  Eliminar", new Point(16, 136));
            chkTodos = MkCheck("✅  Todos", new Point(16, 168));

            groupPermisos.Controls.AddRange(new Control[]
            {
                lblNombreUsuario,
                chkVer, chkEditar, chkEliminar, chkTodos
            });

            Button btnGestionar = MkBtn(groupPermisos, "⚙  Gestionar Usuarios",
                new Point(8, 220), new Size(202, 30),
                Color.FromArgb(80, 80, 160));
            btnGestionar.ForeColor = Color.White;
            btnGestionar.Click += BtnGestionarUsuarios_Click;

            // ── 6. Log ────────────────────────────────────────────
            GroupBox grpLog = new GroupBox
            {
                Text = "📋  Log FTP",
                Location = new Point(966, 358),
                Size = new Size(218, 210),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lstLog = new ListBox
            {
                Location = new Point(6, 22),
                Size = new Size(206, 178),
                Font = new Font("Consolas", 7),
                HorizontalScrollbar = true,
                BackColor = Color.FromArgb(15, 15, 30),
                ForeColor = Color.Gold
            };
            grpLog.Controls.Add(lstLog);
            // ── 7. Editor de .txt ─────────────────────────────────────────
            groupEditor = new GroupBox
            {
                Text = "📝  Editor de archivo .txt",
                Location = new Point(12, 580),
                Size = new Size(940, 130),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblArchivoEdit = new Label
            {
                Text = "Haz doble clic en un archivo .txt para editarlo",
                Location = new Point(8, 22),
                Size = new Size(920, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            txtContenido = new TextBox
            {
                Location = new Point(8, 44),
                Size = new Size(820, 76),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            btnGuardarTxt = new Button
            {
                Text = "💾\nGuardar",
                Location = new Point(838, 44),
                Size = new Size(94, 76),
                BackColor = Color.FromArgb(0, 170, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false
            };
            btnGuardarTxt.FlatAppearance.BorderSize = 0;
            btnGuardarTxt.Click += BtnGuardarTxt_Click;

            groupEditor.Controls.AddRange(new Control[] { lblArchivoEdit, txtContenido, btnGuardarTxt });
            this.Controls.Add(groupEditor);

            // Agrandar el form para que quepa el editor
            this.Size = new Size(1200, 780);
            this.MinimumSize = new Size(1100, 760);

            // Doble clic en árboles para abrir editor
            treeLocal.NodeMouseDoubleClick += TreeLocal_DoubleClick;
            treeRemoto.NodeMouseDoubleClick += TreeRemoto_DoubleClick;
            // ── Nota: ruta del disco ──────────────────────────────
            Label lblRuta = new Label
            {
                Text = $"📂  Disco: {FTPManager.RutaBase}",
                Location = new Point(12, 578),
                Size = new Size(940, 22),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.DarkSlateGray
            };

            // ── Agregar todo al Form ──────────────────────────────
            this.Controls.AddRange(new Control[]
            {
                panelCon, grpRemoto, panelCentro, grpLocal,
                groupPermisos, grpLog, lblRuta
            });
        }

        // ═════════════════════════════════════════════════════════
        //  Carga de datos
        // ═════════════════════════════════════════════════════════
        private void CargarCombos()
        {
            cmbClienteLocal.Items.Clear();
            cmbServidor.Items.Clear();

            foreach (var c in dhcpManager.Clientes)
            {
                cmbClienteLocal.Items.Add(c);
                cmbServidor.Items.Add(c);
            }

            if (cmbClienteLocal.Items.Count > 0) cmbClienteLocal.SelectedIndex = 0;
            if (cmbServidor.Items.Count > 1) cmbServidor.SelectedIndex = 1;
        }

        private void CargarArbolLocal()
        {
            treeLocal.Nodes.Clear();
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            if (cliente == null) return;

            string ruta = ftpManager.ObtenerRutaCliente(cliente.Hostname);
            Directory.CreateDirectory(ruta);

            var raiz = new TreeNode($"🏠 {cliente.Hostname}") { Tag = ruta };
            PopularNodo(raiz, ruta);
            treeLocal.Nodes.Add(raiz);
            treeLocal.ExpandAll();

            lblInfoLocal.Text = $"🖥  {cliente.Hostname}   IP: {cliente.IP}   ·   {ruta}";
            lblInfoLocal.ForeColor = Color.DarkGreen;
        }

        private void CargarArbolRemoto()
        {
            treeRemoto.Nodes.Clear();
            var con = ftpManager.ConexionActiva;
            if (con == null) return;

            string ruta = ftpManager.ObtenerRutaCliente(con.Servidor.Hostname);
            var raiz = new TreeNode($"🖧 {con.Servidor.Hostname}") { Tag = ruta };
            PopularNodo(raiz, ruta);
            treeRemoto.Nodes.Add(raiz);
            treeRemoto.ExpandAll();

            lblInfoRemoto.Text =
                $"🖧  {con.Servidor.Hostname}   IP: {con.Servidor.IP}" +
                $"   Usuario: {con.Usuario.Username}";
            lblInfoRemoto.ForeColor = Color.DarkRed;
        }

        private void PopularNodo(TreeNode nodo, string ruta)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(ruta))
                {
                    var sub = new TreeNode($"📂 {Path.GetFileName(dir)}") { Tag = dir };
                    PopularNodo(sub, dir);
                    nodo.Nodes.Add(sub);
                }
                foreach (string file in Directory.GetFiles(ruta))
                {
                    nodo.Nodes.Add(new TreeNode(
                        $"{IconoPorExtension(Path.GetExtension(file))} {Path.GetFileName(file)}")
                    {
                        Tag = file
                    });
                }
            }
            catch { /* acceso denegado – ignorar */ }
        }

        private static string IconoPorExtension(string ext)
        {
            switch (ext.ToLower())
            {
                case ".txt": return "📄";
                case ".png": case ".jpg": case ".jpeg": case ".bmp": return "🖼";
                case ".mp3": case ".wav": case ".ogg": return "🎵";
                case ".mp4": case ".avi": case ".mkv": return "🎬";
                case ".pdf": return "📕";
                case ".zip": case ".rar": case ".7z": return "📦";
                case ".cs": return "⌨";
                default: return "📎";
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Botón Conectar
        // ═════════════════════════════════════════════════════════
        private void BtnConectar_Click(object sender, EventArgs e)
        {
            var clienteLocal = cmbClienteLocal.SelectedItem as ClienteDHCP;
            var servidor = cmbServidor.SelectedItem as ClienteDHCP;

            if (clienteLocal == null || servidor == null)
            { MsgAviso("Selecciona un cliente y un servidor."); return; }

            if (clienteLocal.Hostname == servidor.Hostname)
            { MsgAviso("No puedes conectarte a ti mismo."); return; }

            // ── Diálogo de login (con reintento) ──────────────────
            FTPUsuario usuario = null;
            using (var dlg = new FormLoginFTP(servidor.Hostname))
            {
                while (true)
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    try
                    {
                        usuario = ftpManager.Autenticar(
                            servidor.Hostname, dlg.Usuario, dlg.Contrasena);
                        break;
                    }
                    catch (Exception ex) { dlg.MostrarError(ex.Message); }
                }
            }

            ftpManager.Conectar(clienteLocal, servidor, usuario);

            // ── Actualizar UI ─────────────────────────────────────
            lblEstado.Text = $"✅  {clienteLocal.Hostname} → {servidor.Hostname}";
            lblEstado.ForeColor = Color.Lime;
            btnConectar.Enabled = false;
            btnDesconectar.Enabled = true;
            cmbClienteLocal.Enabled = cmbServidor.Enabled = false;

            treeRemoto.Enabled = true;
            btnCrearCarpetaRemoto.Enabled = usuario.PuedeEditar();
            btnCrearArchivoRemoto.Enabled = usuario.PuedeEditar();
            btnEliminarRemoto.Enabled = usuario.PuedeEliminar();
            btnTransferir.Enabled = usuario.PuedeVer();
            btnEnviar.Enabled = usuario.PuedeEditar();

            lblConectadoComo.Text = $"Conectado como\n{usuario.Username}";
            lblConectadoComo.ForeColor = Color.DarkGreen;
            lblConectadoComo.Font = new Font("Segoe UI", 8, FontStyle.Bold);

            ActualizarPermisos(usuario);
            CargarArbolRemoto();
        }

        // ═════════════════════════════════════════════════════════
        //  Botón Desconectar
        // ═════════════════════════════════════════════════════════
        private void BtnDesconectar_Click(object sender, EventArgs e)
        {
            ftpManager.Desconectar();

            lblEstado.Text = "⭕  Sin conexión";
            lblEstado.ForeColor = Color.Tomato;
            btnConectar.Enabled = true;
            btnDesconectar.Enabled = false;
            cmbClienteLocal.Enabled = cmbServidor.Enabled = true;

            treeRemoto.Nodes.Clear();
            treeRemoto.Enabled = false;
            btnCrearCarpetaRemoto.Enabled =
            btnCrearArchivoRemoto.Enabled =
            btnEliminarRemoto.Enabled =
            btnTransferir.Enabled =
            btnEnviar.Enabled = false;

            lblConectadoComo.Text = "Sin conexión";
            lblConectadoComo.ForeColor = Color.Gray;
            lblConectadoComo.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblInfoRemoto.Text = "Sin conexión FTP";
            lblInfoRemoto.ForeColor = Color.Gray;

            LimpiarPermisos();
        }

        // ═════════════════════════════════════════════════════════
        //  Transferir / Enviar
        // ═════════════════════════════════════════════════════════
        private void BtnTransferir_Click(object sender, EventArgs e)
        {
            string src = treeRemoto.SelectedNode?.Tag as string;
            if (string.IsNullOrEmpty(src))
            { MsgAviso("Selecciona un archivo o carpeta del panel Remoto para transferir."); return; }

            try
            {
                var local = cmbClienteLocal.SelectedItem as ClienteDHCP;

                if (File.Exists(src))
                {
                    ftpManager.Transferir(src, local.Hostname);
                    MessageBox.Show(
                        $"Archivo '{Path.GetFileName(src)}' transferido a tu carpeta local.",
                        "Transferencia completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (Directory.Exists(src))
                {
                    ftpManager.TransferirCarpeta(src, local.Hostname);
                    MessageBox.Show(
                        $"Carpeta '{Path.GetFileName(src)}' y todo su contenido transferidos.",
                        "Transferencia completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MsgAviso("El elemento seleccionado ya no existe en el servidor.");
                    return;
                }

                CargarArbolLocal();
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }

        private void BtnEnviar_Click(object sender, EventArgs e)
        {
            string src = treeLocal.SelectedNode?.Tag as string;
            if (string.IsNullOrEmpty(src))
            { MsgAviso("Selecciona un archivo o carpeta del panel Local para enviar."); return; }

            try
            {
                string hostnameServidor = ftpManager.ConexionActiva.Servidor.Hostname;

                if (File.Exists(src))
                {
                    ftpManager.Enviar(src, hostnameServidor);
                    MessageBox.Show(
                        $"Archivo '{Path.GetFileName(src)}' enviado al servidor.",
                        "Envío completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (Directory.Exists(src))
                {
                    ftpManager.EnviarCarpeta(src, hostnameServidor);
                    MessageBox.Show(
                        $"Carpeta '{Path.GetFileName(src)}' y todo su contenido enviados.",
                        "Envío completo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MsgAviso("El elemento seleccionado ya no existe.");
                    return;
                }

                CargarArbolRemoto();
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }

        // ═════════════════════════════════════════════════════════
        //  Operaciones REMOTO
        // ═════════════════════════════════════════════════════════
        private void BtnCrearCarpetaRemoto_Click(object sender, EventArgs e)
        {
            string nombre = PedirNombre("Nueva Carpeta en Servidor", "Nombre de la carpeta:");
            if (nombre == null) return;
            try
            {
                string padre = CarpetaPadreDesdeTree(treeRemoto,
                    ftpManager.ConexionActiva.Servidor.Hostname);
                ftpManager.CrearCarpeta(Path.Combine(padre, nombre));
                CargarArbolRemoto();
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }

        private void BtnCrearArchivoRemoto_Click(object sender, EventArgs e)
        {
            string nombre = PedirNombre("Nuevo Archivo en Servidor",
                "Nombre del archivo (incluye extensión, ej: nota.txt):");
            if (nombre == null) return;
            try
            {
                string padre = CarpetaPadreDesdeTree(treeRemoto,
                    ftpManager.ConexionActiva.Servidor.Hostname);
                ftpManager.CrearArchivo(Path.Combine(padre, nombre),
                    $"Creado remotamente vía FTP — {DateTime.Now}");
                CargarArbolRemoto();
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }

        private void BtnEliminarRemoto_Click(object sender, EventArgs e)
        {
            string ruta = treeRemoto.SelectedNode?.Tag as string;
            if (!EsRutaValida(ruta)) { MsgAviso("Selecciona un elemento para eliminar."); return; }

            string raizServidor = ftpManager.ObtenerRutaCliente(
                ftpManager.ConexionActiva.Servidor.Hostname);
            if (string.Equals(ruta, raizServidor, StringComparison.OrdinalIgnoreCase))
            { MsgError("No puedes eliminar la carpeta raíz del servidor."); return; }

            if (Confirmar($"¿Eliminar '{Path.GetFileName(ruta)}'?"))
            {
                try { ftpManager.EliminarElemento(ruta); CargarArbolRemoto(); }
                catch (Exception ex) { MsgError(ex.Message); }
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Operaciones LOCALES
        // ═════════════════════════════════════════════════════════
        private void BtnCrearCarpetaLocal_Click(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            if (cliente == null) return;
            string nombre = PedirNombre("Nueva Carpeta Local", "Nombre:");
            if (nombre == null) return;
            try
            {
                string padre = CarpetaPadreDesdeTree(treeLocal, cliente.Hostname);
                ftpManager.CrearCarpeta(Path.Combine(padre, nombre));
                CargarArbolLocal();
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }

        private void BtnCrearArchivoLocal_Click(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            if (cliente == null) return;
            string nombre = PedirNombre("Nuevo Archivo Local",
                "Nombre del archivo (incluye extensión, ej: nota.txt):");
            if (nombre == null) return;
            try
            {
                string padre = CarpetaPadreDesdeTree(treeLocal, cliente.Hostname);
                ftpManager.CrearArchivo(Path.Combine(padre, nombre),
                    $"Archivo de {cliente.Hostname} — {DateTime.Now}");
                CargarArbolLocal();
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }

        private void BtnEliminarLocal_Click(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            string ruta = treeLocal.SelectedNode?.Tag as string;

            if (!EsRutaValida(ruta)) { MsgAviso("Selecciona un elemento para eliminar."); return; }

            string raizLocal = ftpManager.ObtenerRutaCliente(cliente?.Hostname ?? "");
            if (string.Equals(ruta, raizLocal, StringComparison.OrdinalIgnoreCase))
            { MsgError("No puedes eliminar tu carpeta raíz."); return; }

            if (Confirmar($"¿Eliminar '{Path.GetFileName(ruta)}'?"))
            {
                try { ftpManager.EliminarElemento(ruta); CargarArbolLocal(); }
                catch (Exception ex) { MsgError(ex.Message); }
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Gestión de usuarios
        // ═════════════════════════════════════════════════════════
        private void BtnGestionarUsuarios_Click(object sender, EventArgs e)
        {
            var servidor = cmbServidor.SelectedItem as ClienteDHCP;
            if (servidor == null) { MsgAviso("Selecciona primero un servidor."); return; }

            using (var dlg = new FormGestionUsuariosFTP(ftpManager, servidor.Hostname))
                dlg.ShowDialog(this);
        }

        // ═════════════════════════════════════════════════════════
        //  Permisos (solo lectura — refleja al usuario conectado)
        // ═════════════════════════════════════════════════════════
        private void ActualizarPermisos(FTPUsuario usuario)
        {
            lblNombreUsuario.Text = usuario.Username;
            chkVer.Checked = usuario.PuedeVer();
            chkEditar.Checked = usuario.PuedeEditar();
            chkEliminar.Checked = usuario.PuedeEliminar();
            chkTodos.Checked = usuario.Permisos == FTPPermiso.Todos;
        }

        private void LimpiarPermisos()
        {
            chkVer.Checked = false;
            chkEditar.Checked = false;
            chkEliminar.Checked = false;
            chkTodos.Checked = false;
            lblNombreUsuario.Text = "—";
        }

        // ═════════════════════════════════════════════════════════
        //  Utilidades
        // ═════════════════════════════════════════════════════════
        private string CarpetaPadreDesdeTree(TreeView tree, string hostname)
        {
            string raiz = ftpManager.ObtenerRutaCliente(hostname);
            string tag = tree.SelectedNode?.Tag as string;
            if (tag == null) return raiz;
            if (Directory.Exists(tag)) return tag;
            if (File.Exists(tag)) return Path.GetDirectoryName(tag) ?? raiz;
            return raiz;
        }

        private bool EsRutaValida(string ruta) =>
            !string.IsNullOrEmpty(ruta) && Path.IsPathRooted(ruta) &&
            (File.Exists(ruta) || Directory.Exists(ruta));

        private string PedirNombre(string titulo, string etiqueta)
        {
            using (var dlg = new Form())
            {
                dlg.Text = titulo; dlg.Size = new Size(315, 138);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;

                var lbl = new Label { Text = etiqueta, Location = new Point(10, 12), Size = new Size(290, 20) };
                var txt = new TextBox { Location = new Point(10, 36), Size = new Size(285, 24) };
                var ok = new Button { Text = "Aceptar", Location = new Point(80, 72), Size = new Size(80, 28), DialogResult = DialogResult.OK };
                var cx = new Button { Text = "Cancelar", Location = new Point(175, 72), Size = new Size(80, 28), DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cx });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cx;

                return dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text)
                       ? txt.Text.Trim()
                       : null;
            }
        }

        private bool Confirmar(string msg) =>
            MessageBox.Show(msg, "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        private void MsgAviso(string msg) =>
            MessageBox.Show(msg, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void MsgError(string msg) =>
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private void RegistrarLog(string msg)
        {
            string entrada = $"[{DateTime.Now:HH:mm:ss}]  {msg}";
            lstLog.Items.Add(entrada);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        private void SafeInvoke(Action a)
        {
            if (this.InvokeRequired) this.Invoke(a); else a();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ftpManager.Desconectar();
            base.OnFormClosing(e);
        }

        // ── Helpers de construcción de controles ──────────────────
        private static void AddLabel(Control parent, string text, Point loc,
            string font, float size, FontStyle style, Color fore)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = loc,
                Size = new Size(180, 28),
                Font = new Font(font, size, style),
                ForeColor = fore,
                BackColor = Color.Transparent,
                AutoSize = true
            });
        }

        private static Button MkBtn(Control parent, string text, Point loc,
            Size size, Color back)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                BackColor = back,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            parent.Controls.Add(btn);
            return btn;
        }

        private static CheckBox MkCheck(string text, Point loc) =>
            new CheckBox
            {
                Text = text,
                Location = loc,
                Size = new Size(190, 26),
                Font = new Font("Segoe UI", 9),
                Enabled = false          // solo lectura: refleja permisos del usuario
            };
        // ═════════════════════════════════════════════════════════
        //  Editor de .txt
        // ═════════════════════════════════════════════════════════
        private void TreeLocal_DoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            string ruta = e.Node.Tag as string;
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) return;

            if (!ruta.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            { MsgAviso("Solo se pueden editar archivos .txt"); return; }

            AbrirEnEditor(ruta, esRemoto: false);
        }

        private void TreeRemoto_DoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (ftpManager.ConexionActiva == null) return;

            if (!ftpManager.ConexionActiva.Usuario.PuedeEditar())
            { MsgAviso("No tienes permiso 'Editar' para modificar archivos del servidor."); return; }

            string ruta = e.Node.Tag as string;
            if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) return;

            if (!ruta.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            { MsgAviso("Solo se pueden editar archivos .txt"); return; }

            AbrirEnEditor(ruta, esRemoto: true);
        }

        private void AbrirEnEditor(string ruta, bool esRemoto)
        {
            rutaArchivoEditando = ruta;

            string origen = esRemoto
                ? $"🖧 Servidor · {ftpManager.ConexionActiva?.Servidor.Hostname}"
                : $"🖥 Local · {(cmbClienteLocal.SelectedItem as ClienteDHCP)?.Hostname}";

            lblArchivoEdit.Text = $"{origen}  ›  {Path.GetFileName(ruta)}   |   " +
                                       $"Modificado: {File.GetLastWriteTime(ruta):dd/MM/yyyy HH:mm}";
            lblArchivoEdit.ForeColor = esRemoto ? Color.DarkRed : Color.DarkBlue;

            txtContenido.Text = File.ReadAllText(ruta);
            txtContenido.ReadOnly = false;
            txtContenido.BackColor = Color.White;
            btnGuardarTxt.Enabled = true;
        }

        private void BtnGuardarTxt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rutaArchivoEditando)) return;

            try
            {
                File.WriteAllText(rutaArchivoEditando, txtContenido.Text);
                RegistrarLog($"💾 Guardado: {Path.GetFileName(rutaArchivoEditando)}");

                lblArchivoEdit.Text = lblArchivoEdit.Text.Replace(
                    $"Modificado: {File.GetLastWriteTime(rutaArchivoEditando).AddSeconds(-1):dd/MM/yyyy HH:mm}",
                    $"Modificado: {DateTime.Now:dd/MM/yyyy HH:mm}");

                // Refrescar el árbol correspondiente
                bool esRemoto = rutaArchivoEditando.Contains(
                    ftpManager.ConexionActiva?.Servidor.Hostname ?? "\0");

                if (esRemoto) CargarArbolRemoto(); else CargarArbolLocal();

                MessageBox.Show("Archivo guardado correctamente.", "Guardado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MsgError(ex.Message); }
        }
    }
}