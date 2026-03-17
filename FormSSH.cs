using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public partial class FormSSH : Form
    {
        // ── Managers ──────────────────────────────────────────────
        private readonly SSHManager sshManager;
        private readonly DHCPManager dhcpManager;

        // ── Panel superior ────────────────────────────────────────
        private ComboBox cmbClienteLocal;
        private ComboBox cmbServidor;
        private Button btnConectar;
        private Button btnDesconectar;
        private Label lblEstadoConexion;

        // ── Árbol LOCAL ───────────────────────────────────────────
        private TreeView treeLocal;
        private Button btnCrearCarpetaLocal;
        private Button btnCrearArchivoLocal;
        private Button btnEliminarLocal;
        private Label lblInfoLocal;

        // ── Árbol SERVIDOR ────────────────────────────────────────
        private GroupBox groupServidor;
        private TreeView treeServidor;
        private Button btnCrearCarpetaServidor;
        private Button btnCrearArchivoServidor;
        private Button btnEliminarServidor;
        private Label lblInfoServidor;

        // ── Editor de contenido ───────────────────────────────────
        private TextBox txtContenido;
        private Button btnGuardar;
        private Label lblArchivoEdit;

        // ── Log de actividad ──────────────────────────────────────
        private ListBox lstLog;

        // ── Estado de selección ───────────────────────────────────
        private SSHArchivo archivoSeleccionado;
        private SSHCarpeta carpetaContextoLocal;     // carpeta seleccionada / activa en árbol local
        private SSHCarpeta carpetaContextoServidor;  // carpeta seleccionada / activa en árbol servidor
        private bool edicionEsDeServidor;

        // ─────────────────────────────────────────────────────────
        public FormSSH(DHCPManager manager)
        {
            dhcpManager = manager;
            sshManager = new SSHManager();
            sshManager.InicializarClientesSistemaArchivos(dhcpManager.Clientes);
            sshManager.AccionRealizada += (msg) => SafeInvoke(() => RegistrarLog(msg));

            InicializarUI();
            CargarComboClientes();
        }

        // ═════════════════════════════════════════════════════════
        //  Construcción de la interfaz
        // ═════════════════════════════════════════════════════════
        private void InicializarUI()
        {
            this.Text = "Simulador de Redes — SSH";
            this.Size = new Size(1150, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1050, 700);

            // ── 1. Panel de conexión (banda superior) ─────────────
            Panel panelConexion = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(1112, 58),
                BackColor = Color.FromArgb(30, 30, 60),
                BorderStyle = BorderStyle.None
            };

            Label lblTitulo = new Label
            {
                Text = "🔐  SSH",
                Location = new Point(12, 12),
                Size = new Size(70, 32),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.LimeGreen,
                BackColor = Color.Transparent
            };

            Label lblLblCliente = new Label
            {
                Text = "Cliente:",
                Location = new Point(100, 18),
                Size = new Size(55, 22),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9)
            };

            cmbClienteLocal = new ComboBox
            {
                Location = new Point(158, 15),
                Size = new Size(190, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbClienteLocal.SelectedIndexChanged += CmbClienteLocal_Changed;

            Label lblFlecha = new Label
            {
                Text = "──SSH──▶",
                Location = new Point(358, 18),
                Size = new Size(90, 22),
                ForeColor = Color.LimeGreen,
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label lblLblServidor = new Label
            {
                Text = "Servidor:",
                Location = new Point(455, 18),
                Size = new Size(60, 22),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9)
            };

            cmbServidor = new ComboBox
            {
                Location = new Point(518, 15),
                Size = new Size(190, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };

            btnConectar = new Button
            {
                Text = "🔗  Conectar",
                Location = new Point(720, 13),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(0, 180, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnConectar.FlatAppearance.BorderSize = 0;
            btnConectar.Click += BtnConectar_Click;

            btnDesconectar = new Button
            {
                Text = "✂  Desconectar",
                Location = new Point(850, 13),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnDesconectar.FlatAppearance.BorderSize = 0;
            btnDesconectar.Click += BtnDesconectar_Click;

            lblEstadoConexion = new Label
            {
                Text = "⭕  Sin conexión",
                Location = new Point(990, 18),
                Size = new Size(115, 22),
                ForeColor = Color.Tomato,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };

            panelConexion.Controls.AddRange(new Control[] {
                lblTitulo, lblLblCliente, cmbClienteLocal,
                lblFlecha, lblLblServidor, cmbServidor,
                btnConectar, btnDesconectar, lblEstadoConexion
            });

            // ── 2. Panel LOCAL ────────────────────────────────────
            GroupBox groupLocal = new GroupBox
            {
                Text = "📁  Mi Sistema de Archivos  (local)",
                Location = new Point(12, 80),
                Size = new Size(530, 485),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblInfoLocal = new Label
            {
                Text = "Selecciona un cliente",
                Location = new Point(8, 22),
                Size = new Size(510, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            treeLocal = new TreeView
            {
                Location = new Point(8, 44),
                Size = new Size(510, 380),
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(245, 250, 255),
                BorderStyle = BorderStyle.FixedSingle
            };
            treeLocal.AfterSelect += TreeLocal_AfterSelect;
            treeLocal.NodeMouseDoubleClick += TreeLocal_DoubleClick;

            // Barra de botones locales
            btnCrearCarpetaLocal = CreaBoton("📁  Carpeta", Color.FromArgb(255, 220, 80), new Point(8, 432));
            btnCrearArchivoLocal = CreaBoton("📄  Archivo TXT", Color.FromArgb(100, 200, 255), new Point(138, 432));
            btnEliminarLocal = CreaBoton("🗑  Eliminar", Color.FromArgb(255, 120, 100), new Point(278, 432));

            btnCrearCarpetaLocal.Click += BtnCrearCarpetaLocal_Click;
            btnCrearArchivoLocal.Click += BtnCrearArchivoLocal_Click;
            btnEliminarLocal.Click += BtnEliminarLocal_Click;

            groupLocal.Controls.AddRange(new Control[] {
                lblInfoLocal, treeLocal,
                btnCrearCarpetaLocal, btnCrearArchivoLocal, btnEliminarLocal
            });

            // ── 3. Panel SERVIDOR ─────────────────────────────────
            groupServidor = new GroupBox
            {
                Text = "🖧  Sistema de Archivos del Servidor  (remoto)",
                Location = new Point(555, 80),
                Size = new Size(569, 485),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblInfoServidor = new Label
            {
                Text = "Sin conexión SSH activa",
                Location = new Point(8, 22),
                Size = new Size(549, 18),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            treeServidor = new TreeView
            {
                Location = new Point(8, 44),
                Size = new Size(549, 380),
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(255, 248, 245),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            treeServidor.AfterSelect += TreeServidor_AfterSelect;
            treeServidor.NodeMouseDoubleClick += TreeServidor_DoubleClick;

            // Barra de botones servidor
            btnCrearCarpetaServidor = CreaBoton("📁  Carpeta", Color.FromArgb(255, 220, 80), new Point(8, 432));
            btnCrearArchivoServidor = CreaBoton("📄  Archivo TXT", Color.FromArgb(100, 200, 255), new Point(138, 432));
            btnEliminarServidor = CreaBoton("🗑  Eliminar", Color.FromArgb(255, 120, 100), new Point(278, 432));

            btnCrearCarpetaServidor.Enabled = false;
            btnCrearArchivoServidor.Enabled = false;
            btnEliminarServidor.Enabled = false;

            btnCrearCarpetaServidor.Click += BtnCrearCarpetaServidor_Click;
            btnCrearArchivoServidor.Click += BtnCrearArchivoServidor_Click;
            btnEliminarServidor.Click += BtnEliminarServidor_Click;

            groupServidor.Controls.AddRange(new Control[] {
                lblInfoServidor, treeServidor,
                btnCrearCarpetaServidor, btnCrearArchivoServidor, btnEliminarServidor
            });

            // ── 4. Editor de contenido ────────────────────────────
            GroupBox groupEditor = new GroupBox
            {
                Text = "📝  Editor de archivo .txt",
                Location = new Point(12, 575),
                Size = new Size(840, 148),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblArchivoEdit = new Label
            {
                Text = "Haz doble clic en un archivo .txt para editar su contenido",
                Location = new Point(8, 22),
                Size = new Size(820, 18),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8, FontStyle.Italic)
            };

            txtContenido = new TextBox
            {
                Location = new Point(8, 44),
                Size = new Size(710, 90),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            btnGuardar = new Button
            {
                Text = "💾\nGuardar",
                Location = new Point(728, 44),
                Size = new Size(100, 90),
                BackColor = Color.FromArgb(0, 180, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            groupEditor.Controls.AddRange(new Control[] { lblArchivoEdit, txtContenido, btnGuardar });

            // ── 5. Log de actividad ───────────────────────────────
            GroupBox groupLog = new GroupBox
            {
                Text = "📋  Log SSH",
                Location = new Point(862, 575),
                Size = new Size(262, 148),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lstLog = new ListBox
            {
                Location = new Point(6, 20),
                Size = new Size(250, 118),
                Font = new Font("Consolas", 7),
                HorizontalScrollbar = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LimeGreen
            };

            groupLog.Controls.Add(lstLog);

            // ── Agregar todo al formulario ────────────────────────
            this.Controls.AddRange(new Control[] {
                panelConexion, groupLocal, groupServidor, groupEditor, groupLog
            });
        }

        // ═════════════════════════════════════════════════════════
        //  Carga de combos
        // ═════════════════════════════════════════════════════════
        private void CargarComboClientes()
        {
            cmbClienteLocal.Items.Clear();
            cmbServidor.Items.Clear();

            foreach (var c in dhcpManager.Clientes)
            {
                cmbClienteLocal.Items.Add(c);
                cmbServidor.Items.Add(c);
            }

            if (cmbClienteLocal.Items.Count > 0)
            {
                cmbClienteLocal.SelectedIndex = 0;
                // SelectedIndexChanged actualizará el árbol local
            }
        }

        private void RefrescarComboServidor(ClienteDHCP excluir)
        {
            var actual = cmbServidor.SelectedItem as ClienteDHCP;
            cmbServidor.Items.Clear();

            foreach (var c in dhcpManager.Clientes.Where(x => x != excluir))
                cmbServidor.Items.Add(c);

            if (actual != null && cmbServidor.Items.Contains(actual))
                cmbServidor.SelectedItem = actual;
            else if (cmbServidor.Items.Count > 0)
                cmbServidor.SelectedIndex = 0;
        }

        // ═════════════════════════════════════════════════════════
        //  Construcción de árboles
        // ═════════════════════════════════════════════════════════
        private void CargarArbolLocal(ClienteDHCP cliente)
        {
            treeLocal.Nodes.Clear();
            if (cliente == null) return;

            var raiz = sshManager.ObtenerCarpetaRaiz(cliente);
            carpetaContextoLocal = raiz;
            treeLocal.Nodes.Add(NodoDeCarpeta(raiz, esRaiz: true));
            treeLocal.ExpandAll();

            lblInfoLocal.Text = $"🖥  {cliente.Hostname}   ·   IP: {cliente.IP}   ·   {raiz.ContarTotalArchivos()} archivo(s)";
            lblInfoLocal.ForeColor = Color.DarkBlue;
        }

        private void CargarArbolServidor(ClienteDHCP servidor)
        {
            treeServidor.Nodes.Clear();
            if (servidor == null) return;

            var raiz = sshManager.ObtenerCarpetaRaiz(servidor);
            carpetaContextoServidor = raiz;
            treeServidor.Nodes.Add(NodoDeCarpeta(raiz, esRaiz: true));
            treeServidor.ExpandAll();

            lblInfoServidor.Text = $"🖧  {servidor.Hostname}   ·   IP: {servidor.IP}   ·   {raiz.ContarTotalArchivos()} archivo(s)";
            lblInfoServidor.ForeColor = Color.DarkRed;
        }

        private TreeNode NodoDeCarpeta(SSHCarpeta carpeta, bool esRaiz = false)
        {
            string icono = esRaiz ? "🏠" : "📂";
            var nodo = new TreeNode($"{icono} {carpeta.Nombre}")
            {
                Tag = carpeta,
                ForeColor = Color.FromArgb(30, 80, 150)
            };

            foreach (var sub in carpeta.Subcarpetas)
                nodo.Nodes.Add(NodoDeCarpeta(sub));

            foreach (var arch in carpeta.Archivos)
                nodo.Nodes.Add(NodoDeArchivo(arch));

            return nodo;
        }

        private TreeNode NodoDeArchivo(SSHArchivo archivo)
        {
            return new TreeNode($"📄 {archivo.Nombre}")
            {
                Tag = archivo,
                ForeColor = Color.FromArgb(30, 130, 60)
            };
        }

        // ═════════════════════════════════════════════════════════
        //  Eventos de selección en los árboles
        // ═════════════════════════════════════════════════════════
        private void TreeLocal_AfterSelect(object sender, TreeViewEventArgs e)
        {
            edicionEsDeServidor = false;
            if (e.Node.Tag is SSHCarpeta c)
            {
                carpetaContextoLocal = c;
                archivoSeleccionado = null;
                LimpiarEditor();
            }
            else if (e.Node.Tag is SSHArchivo a)
            {
                archivoSeleccionado = a;
                carpetaContextoLocal = e.Node.Parent?.Tag as SSHCarpeta;
                // Solo mostrar info, edición por doble clic
                lblArchivoEdit.Text = $"📄  {a.Nombre}   ·   {a.FechaModificacion:dd/MM/yyyy HH:mm}   — doble clic para editar";
                lblArchivoEdit.ForeColor = Color.DarkBlue;
            }
        }

        private void TreeServidor_AfterSelect(object sender, TreeViewEventArgs e)
        {
            edicionEsDeServidor = true;
            if (e.Node.Tag is SSHCarpeta c)
            {
                carpetaContextoServidor = c;
                archivoSeleccionado = null;
                LimpiarEditor();
            }
            else if (e.Node.Tag is SSHArchivo a)
            {
                archivoSeleccionado = a;
                carpetaContextoServidor = e.Node.Parent?.Tag as SSHCarpeta;
                lblArchivoEdit.Text = $"📄  {a.Nombre}   ·   {a.FechaModificacion:dd/MM/yyyy HH:mm}   — doble clic para editar";
                lblArchivoEdit.ForeColor = Color.DarkRed;
            }
        }

        private void TreeLocal_DoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is SSHArchivo arch)
                AbrirEnEditor(arch, esServidor: false);
        }

        private void TreeServidor_DoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is SSHArchivo arch)
                AbrirEnEditor(arch, esServidor: true);
        }

        private void AbrirEnEditor(SSHArchivo archivo, bool esServidor)
        {
            archivoSeleccionado = archivo;
            edicionEsDeServidor = esServidor;

            string origen = esServidor
                ? $"🖧 Servidor · {sshManager.ConexionActiva?.Servidor.Hostname}"
                : $"🖥 Local · {(cmbClienteLocal.SelectedItem as ClienteDHCP)?.Hostname}";

            lblArchivoEdit.Text = $"{origen}  ›  {archivo.Nombre}   |   Creado: {archivo.FechaCreacion:dd/MM HH:mm}   Modificado: {archivo.FechaModificacion:dd/MM HH:mm}";
            lblArchivoEdit.ForeColor = esServidor ? Color.DarkRed : Color.DarkBlue;

            txtContenido.Text = archivo.Contenido;
            txtContenido.ReadOnly = false;
            txtContenido.BackColor = Color.White;
            btnGuardar.Enabled = true;
        }

        // ═════════════════════════════════════════════════════════
        //  Botones de CONEXIÓN SSH
        // ═════════════════════════════════════════════════════════
        private void BtnConectar_Click(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            var servidor = cmbServidor.SelectedItem as ClienteDHCP;

            if (cliente == null || servidor == null)
            {
                MessageBox.Show("Selecciona un cliente y un servidor.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                sshManager.Conectar(cliente, servidor);

                // Actualizar UI
                lblEstadoConexion.Text = $"✅ {cliente.Hostname}→{servidor.Hostname}";
                lblEstadoConexion.ForeColor = Color.LimeGreen;
                btnConectar.Enabled = false;
                btnDesconectar.Enabled = true;
                cmbClienteLocal.Enabled = false;
                cmbServidor.Enabled = false;

                treeServidor.Enabled = true;
                btnCrearCarpetaServidor.Enabled = true;
                btnCrearArchivoServidor.Enabled = true;
                btnEliminarServidor.Enabled = true;

                groupServidor.Text = $"🖧  Servidor: {servidor.Hostname}  ({servidor.IP})  — CONECTADO";
                CargarArbolServidor(servidor);

                RegistrarLog($"✅ {cliente.Hostname} conectado a {servidor.Hostname}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error de Conexión SSH",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDesconectar_Click(object sender, EventArgs e)
        {
            sshManager.Desconectar();

            lblEstadoConexion.Text = "⭕  Sin conexión";
            lblEstadoConexion.ForeColor = Color.Tomato;
            btnConectar.Enabled = true;
            btnDesconectar.Enabled = false;
            cmbClienteLocal.Enabled = true;
            cmbServidor.Enabled = true;

            treeServidor.Enabled = false;
            treeServidor.Nodes.Clear();
            btnCrearCarpetaServidor.Enabled = false;
            btnCrearArchivoServidor.Enabled = false;
            btnEliminarServidor.Enabled = false;

            groupServidor.Text = "🖧  Sistema de Archivos del Servidor  (remoto)";
            lblInfoServidor.Text = "Sin conexión SSH activa";
            lblInfoServidor.ForeColor = Color.Gray;

            if (edicionEsDeServidor) LimpiarEditor();

            RegistrarLog("⭕ Desconectado");
        }

        // ═════════════════════════════════════════════════════════
        //  Botones LOCALES
        // ═════════════════════════════════════════════════════════
        private void CmbClienteLocal_Changed(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            if (cliente == null) return;

            sshManager.ObtenerCarpetaRaiz(cliente);
            CargarArbolLocal(cliente);
            RefrescarComboServidor(cliente);
        }

        private void BtnCrearCarpetaLocal_Click(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            if (cliente == null) return;

            string nombre = MostrarDialogoNombre("Nueva Carpeta Local", "Nombre de la carpeta:");
            if (nombre == null) return;

            try
            {
                SSHCarpeta padre = ObtenerCarpetaContextoLocal(cliente);
                sshManager.CrearCarpetaLocal(cliente, nombre, padre);
                CargarArbolLocal(cliente);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void BtnCrearArchivoLocal_Click(object sender, EventArgs e)
        {
            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;
            if (cliente == null) return;

            string nombre = MostrarDialogoNombre("Nuevo Archivo TXT Local", "Nombre del archivo (sin extensión):");
            if (nombre == null) return;

            try
            {
                SSHCarpeta padre = ObtenerCarpetaContextoLocal(cliente);
                sshManager.CrearArchivoLocal(cliente, nombre, padre);
                CargarArbolLocal(cliente);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void BtnEliminarLocal_Click(object sender, EventArgs e)
        {
            if (treeLocal.SelectedNode == null)
            {
                MessageBox.Show("Selecciona un elemento para eliminar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cliente = cmbClienteLocal.SelectedItem as ClienteDHCP;

            if (treeLocal.SelectedNode.Tag is SSHCarpeta carpeta)
            {
                if (carpeta.EsRaiz())
                {
                    MostrarError("No puedes eliminar tu carpeta raíz.");
                    return;
                }
                if (Confirmar($"¿Eliminar la carpeta '{carpeta.Nombre}' y todo su contenido?"))
                {
                    try
                    {
                        sshManager.EliminarCarpetaLocal(carpeta);
                        CargarArbolLocal(cliente);
                        LimpiarEditor();
                    }
                    catch (Exception ex) { MostrarError(ex.Message); }
                }
            }
            else if (treeLocal.SelectedNode.Tag is SSHArchivo archivo)
            {
                SSHCarpeta padre = treeLocal.SelectedNode.Parent?.Tag as SSHCarpeta;
                if (padre == null) return;

                if (Confirmar($"¿Eliminar el archivo '{archivo.Nombre}'?"))
                {
                    sshManager.EliminarArchivoLocal(archivo, padre);
                    CargarArbolLocal(cliente);
                    LimpiarEditor();
                }
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Botones SERVIDOR
        // ═════════════════════════════════════════════════════════
        private void BtnCrearCarpetaServidor_Click(object sender, EventArgs e)
        {
            string nombre = MostrarDialogoNombre("Nueva Carpeta en Servidor", "Nombre de la carpeta:");
            if (nombre == null) return;

            try
            {
                sshManager.CrearCarpetaEnServidor(nombre, carpetaContextoServidor);
                CargarArbolServidor(sshManager.ConexionActiva.Servidor);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void BtnCrearArchivoServidor_Click(object sender, EventArgs e)
        {
            string nombre = MostrarDialogoNombre("Nuevo Archivo TXT en Servidor", "Nombre del archivo (sin extensión):");
            if (nombre == null) return;

            try
            {
                sshManager.CrearArchivoEnServidor(nombre, carpetaContextoServidor);
                CargarArbolServidor(sshManager.ConexionActiva.Servidor);
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void BtnEliminarServidor_Click(object sender, EventArgs e)
        {
            if (treeServidor.SelectedNode == null)
            {
                MessageBox.Show("Selecciona un elemento del servidor para eliminar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (treeServidor.SelectedNode.Tag is SSHCarpeta carpeta)
            {
                if (carpeta.EsRaiz())
                {
                    MostrarError("No puedes eliminar la carpeta raíz del servidor.");
                    return;
                }
                if (Confirmar($"¿Eliminar '{carpeta.Nombre}' y todo su contenido del servidor?"))
                {
                    try
                    {
                        sshManager.EliminarCarpetaEnServidor(carpeta);
                        CargarArbolServidor(sshManager.ConexionActiva.Servidor);
                        LimpiarEditor();
                    }
                    catch (Exception ex) { MostrarError(ex.Message); }
                }
            }
            else if (treeServidor.SelectedNode.Tag is SSHArchivo archivo)
            {
                SSHCarpeta padre = treeServidor.SelectedNode.Parent?.Tag as SSHCarpeta;
                if (padre == null) return;

                if (Confirmar($"¿Eliminar '{archivo.Nombre}' del servidor?"))
                {
                    try
                    {
                        sshManager.EliminarArchivoEnServidor(archivo, padre);
                        CargarArbolServidor(sshManager.ConexionActiva.Servidor);
                        LimpiarEditor();
                    }
                    catch (Exception ex) { MostrarError(ex.Message); }
                }
            }
        }

        // ═════════════════════════════════════════════════════════
        //  Editor
        // ═════════════════════════════════════════════════════════
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (archivoSeleccionado == null) return;

            archivoSeleccionado.Contenido = txtContenido.Text;
            archivoSeleccionado.FechaModificacion = DateTime.Now;

            RegistrarLog($"💾 '{archivoSeleccionado.Nombre}' guardado");

            // Refrescar árbol correspondiente para actualizar fechas
            if (edicionEsDeServidor && sshManager.ConexionActiva != null)
                CargarArbolServidor(sshManager.ConexionActiva.Servidor);
            else
                CargarArbolLocal(cmbClienteLocal.SelectedItem as ClienteDHCP);

            MessageBox.Show("Contenido guardado correctamente.", "Guardado",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LimpiarEditor()
        {
            archivoSeleccionado = null;
            lblArchivoEdit.Text = "Haz doble clic en un archivo .txt para editar su contenido";
            lblArchivoEdit.ForeColor = Color.Gray;
            txtContenido.Text = "";
            txtContenido.ReadOnly = true;
            txtContenido.BackColor = Color.FromArgb(250, 250, 250);
            btnGuardar.Enabled = false;
        }

        // ═════════════════════════════════════════════════════════
        //  Utilidades
        // ═════════════════════════════════════════════════════════

        /// <summary>
        /// Devuelve la carpeta de destino para operaciones locales:
        /// si hay una carpeta seleccionada en el árbol, la usa;
        /// si hay un archivo seleccionado, sube a su padre;
        /// de lo contrario, devuelve la raíz del cliente.
        /// </summary>
        private SSHCarpeta ObtenerCarpetaContextoLocal(ClienteDHCP cliente)
        {
            if (treeLocal.SelectedNode?.Tag is SSHCarpeta c) return c;
            if (treeLocal.SelectedNode?.Tag is SSHArchivo)
                return treeLocal.SelectedNode.Parent?.Tag as SSHCarpeta
                       ?? sshManager.ObtenerCarpetaRaiz(cliente);
            return sshManager.ObtenerCarpetaRaiz(cliente);
        }

        private Button CreaBoton(string texto, Color color, Point ubicacion)
        {
            var btn = new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(120, 30),
                BackColor = color,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private string MostrarDialogoNombre(string titulo, string etiqueta)
        {
            using (var dlg = new Form())
            {
                dlg.Text = titulo;
                dlg.Size = new Size(320, 140);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;

                var lbl = new Label { Text = etiqueta, Location = new Point(10, 15), Size = new Size(280, 20) };
                var txt = new TextBox { Location = new Point(10, 38), Size = new Size(280, 25) };
                var btnOk = new Button { Text = "Aceptar", Location = new Point(90, 75), Size = new Size(80, 27), DialogResult = DialogResult.OK };
                var btnCx = new Button { Text = "Cancelar", Location = new Point(180, 75), Size = new Size(80, 27), DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCx });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCx;

                return dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text)
                    ? txt.Text.Trim()
                    : null;
            }
        }

        private bool Confirmar(string mensaje)
        {
            return MessageBox.Show(mensaje, "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void MostrarError(string mensaje)
        {
            MessageBox.Show(mensaje, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void RegistrarLog(string mensaje)
        {
            string entrada = $"[{DateTime.Now:HH:mm:ss}]  {mensaje}";
            lstLog.Items.Add(entrada);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        private void SafeInvoke(Action accion)
        {
            if (this.InvokeRequired)
                this.Invoke(accion);
            else
                accion();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            sshManager.Desconectar();
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "FormSSH";
            this.ResumeLayout(false);
        }
    }
}
