using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public partial class FormDNS : Form
    {
        private DNSManager dnsManager;

        // Controles principales
        private TreeView treeViewRedes;
        private ListView listViewDispositivos;
        private TextBox txtIdentificador;
        private TextBox txtNombre;
        private TextBox txtIP;
        private TextBox txtMac;
        private ComboBox cmbTipoOrganizacion;
        private ComboBox cmbDominio;
        private Button btnAgregar;
        private Button btnEliminar;
        private Button btnResolver;
        private TextBox txtResolver;
        private Label lblResultado;
        private DataGridView dgvTablaHosts;
        private Button btnSincronizarDHCP;
        private Label lblTotalDispositivos;

        public FormDNS()
        {
            InitializeComponent();
            dnsManager = new DNSManager();

            // Conectar eventos de DNS con Form1
            dnsManager.DispositivoAgregado += OnDispositivoAgregado;
            dnsManager.DispositivoEliminado += OnDispositivoEliminado;
            dnsManager.IPCambio += OnIPCambio;

            // Suscribirse a eventos de DHCP
            if (Form1.DHCPManagerInstance != null)
            {
                Form1.DHCPManagerInstance.ClienteAgregado += OnClienteDHCPAgregado;
                Form1.DHCPManagerInstance.ClienteEliminado += OnClienteDHCPEliminado;
                Form1.DHCPManagerInstance.IPCambio += OnIPCambioDHCP;
            }

            InicializarControles();
            CargarDatos();
        }

        private void InicializarControles()
        {
            this.Size = new Size(1000, 800);
            this.Text = "Simulador de Redes - DNS";
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel izquierdo - Árbol de redes
            GroupBox groupRedes = new GroupBox
            {
                Text = "Estructura de Redes",
                Location = new Point(12, 12),
                Size = new Size(300, 400)
            };

            treeViewRedes = new TreeView
            {
                Location = new Point(6, 20),
                Size = new Size(288, 370),
                ShowNodeToolTips = true
            };
            treeViewRedes.AfterSelect += TreeViewRedes_AfterSelect;
            groupRedes.Controls.Add(treeViewRedes);

            // Panel central - Lista de dispositivos
            GroupBox groupDispositivos = new GroupBox
            {
                Text = "Dispositivos en la Red",
                Location = new Point(320, 12),
                Size = new Size(350, 400)
            };

            listViewDispositivos = new ListView
            {
                Location = new Point(6, 20),
                Size = new Size(338, 340),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listViewDispositivos.Columns.Add("Nombre", 100);
            listViewDispositivos.Columns.Add("IP", 100);
            listViewDispositivos.Columns.Add("Dominio", 80);
            listViewDispositivos.Columns.Add("Tipo", 70);
            listViewDispositivos.SelectedIndexChanged += ListViewDispositivos_SelectedIndexChanged;

            // Label para total de dispositivos
            lblTotalDispositivos = new Label
            {
                Text = "Total: 0 dispositivos",
                Location = new Point(6, 365),
                Size = new Size(338, 25),
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.Blue
            };

            groupDispositivos.Controls.Add(listViewDispositivos);
            groupDispositivos.Controls.Add(lblTotalDispositivos);

            // Panel derecho - Detalles del dispositivo
            GroupBox groupDetalles = new GroupBox
            {
                Text = "Detalles del Dispositivo",
                Location = new Point(680, 12),
                Size = new Size(300, 380)
            };

            int yPos = 25;
            int spacing = 30;

            // Identificador de texto
            Label lblIdentificador = new Label { Text = "Identificador:", Location = new Point(10, yPos), Size = new Size(90, 25) };
            txtIdentificador = new TextBox { Location = new Point(110, yPos), Size = new Size(170, 25), ReadOnly = true };
            groupDetalles.Controls.AddRange(new Control[] { lblIdentificador, txtIdentificador });

            // Nombre
            yPos += spacing;
            Label lblNombre = new Label { Text = "Nombre:", Location = new Point(10, yPos), Size = new Size(90, 25) };
            txtNombre = new TextBox { Location = new Point(110, yPos), Size = new Size(170, 25) };
            groupDetalles.Controls.AddRange(new Control[] { lblNombre, txtNombre });

            // IP
            yPos += spacing;
            Label lblIP = new Label { Text = "IP:", Location = new Point(10, yPos), Size = new Size(90, 25) };
            txtIP = new TextBox { Location = new Point(110, yPos), Size = new Size(170, 25) };
            groupDetalles.Controls.AddRange(new Control[] { lblIP, txtIP });

            // MAC
            yPos += spacing;
            Label lblMac = new Label { Text = "MAC:", Location = new Point(10, yPos), Size = new Size(90, 25) };
            txtMac = new TextBox { Location = new Point(110, yPos), Size = new Size(170, 25) };
            groupDetalles.Controls.AddRange(new Control[] { lblMac, txtMac });

            // Tipo de Organización
            yPos += spacing;
            Label lblTipo = new Label { Text = "Tipo Org:", Location = new Point(10, yPos), Size = new Size(90, 25) };
            cmbTipoOrganizacion = new ComboBox
            {
                Location = new Point(110, yPos),
                Size = new Size(170, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var tipo in Enum.GetNames(typeof(TipoOrganizacion)))
            {
                cmbTipoOrganizacion.Items.Add(tipo);
            }
            cmbTipoOrganizacion.SelectedIndex = 4; // "Otro" por defecto
            groupDetalles.Controls.AddRange(new Control[] { lblTipo, cmbTipoOrganizacion });

            // Dominio
            yPos += spacing;
            Label lblDominio = new Label { Text = "Dominio:", Location = new Point(10, yPos), Size = new Size(90, 25) };
            cmbDominio = new ComboBox
            {
                Location = new Point(110, yPos),
                Size = new Size(170, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbDominio.Items.AddRange(new string[] { "local", "tecnm.mx", "google.com", "empresa.local", "casa.local" });
            cmbDominio.SelectedIndex = 0;
            groupDetalles.Controls.AddRange(new Control[] { lblDominio, cmbDominio });

            // Botones de acción
            yPos += spacing + 10;
            btnAgregar = new Button
            {
                Text = "Agregar Dispositivo",
                Location = new Point(10, yPos),
                Size = new Size(130, 30),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnAgregar.Click += BtnAgregar_Click;

            btnEliminar = new Button
            {
                Text = "Eliminar",
                Location = new Point(150, yPos),
                Size = new Size(80, 30),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnEliminar.Click += BtnEliminar_Click;

            groupDetalles.Controls.AddRange(new Control[] { btnAgregar, btnEliminar });

            // Botón de sincronización (único)
            yPos += spacing;
            btnSincronizarDHCP = new Button
            {
                Text = "🔄 Sincronizar con DHCP",
                Location = new Point(10, yPos),
                Size = new Size(220, 30),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnSincronizarDHCP.Click += BtnSincronizarDHCP_Click;
            groupDetalles.Controls.Add(btnSincronizarDHCP);

            // Panel de Resolución DNS
            GroupBox groupResolver = new GroupBox
            {
                Text = "Resolución de Nombres",
                Location = new Point(12, 420),
                Size = new Size(968, 80)
            };

            Label lblResolver = new Label { Text = "Nombre a resolver:", Location = new Point(10, 30), Size = new Size(120, 25) };
            txtResolver = new TextBox { Location = new Point(140, 30), Size = new Size(200, 25) };
            btnResolver = new Button
            {
                Text = "Resolver",
                Location = new Point(350, 30),
                Size = new Size(100, 25),
                BackColor = Color.LightYellow,
                FlatStyle = FlatStyle.Flat
            };
            btnResolver.Click += BtnResolver_Click;

            lblResultado = new Label
            {
                Text = "IP: ",
                Location = new Point(460, 30),
                Size = new Size(300, 25),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.Green
            };

            groupResolver.Controls.AddRange(new Control[] { lblResolver, txtResolver, btnResolver, lblResultado });

            // Tabla de Hosts
            GroupBox groupHosts = new GroupBox
            {
                Text = "Tabla de Hosts",
                Location = new Point(12, 510),
                Size = new Size(968, 240)
            };

            dgvTablaHosts = new DataGridView
            {
                Location = new Point(6, 20),
                Size = new Size(956, 210),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            DataGridViewTextBoxColumn colNombre = new DataGridViewTextBoxColumn();
            colNombre.Name = "Nombre";
            colNombre.HeaderText = "Nombre Completo";

            DataGridViewTextBoxColumn colIP = new DataGridViewTextBoxColumn();
            colIP.Name = "IP";
            colIP.HeaderText = "Dirección IP";

            dgvTablaHosts.Columns.Add(colNombre);
            dgvTablaHosts.Columns.Add(colIP);

            groupHosts.Controls.Add(dgvTablaHosts);

            // Agregar todos los controles al formulario
            this.Controls.AddRange(new Control[] {
                groupRedes, groupDispositivos, groupDetalles,
                groupResolver, groupHosts
            });
        }

        private void CargarDatos()
        {
            // Cargar árbol de redes
            treeViewRedes.Nodes.Clear();
            foreach (var red in dnsManager.Redes)
            {
                TreeNode nodoRed = CrearNodoArbol(red);
                treeViewRedes.Nodes.Add(nodoRed);
            }
            treeViewRedes.ExpandAll();

            // Cargar lista de dispositivos
            CargarListaDispositivos();

            // Cargar tabla de hosts
            ActualizarTablaHosts();

            // Actualizar contador
            int total = dnsManager.ContarDispositivos();
            lblTotalDispositivos.Text = $"Total: {total} dispositivos";
        }

        private TreeNode CrearNodoArbol(DispositivoDNS dispositivo)
        {
            string tipoIcono = "";
            switch (dispositivo.TipoOrganizacion)
            {
                case TipoOrganizacion.Empresa:
                    tipoIcono = "🏢";
                    break;
                case TipoOrganizacion.Universidad:
                    tipoIcono = "🎓";
                    break;
                case TipoOrganizacion.Gobierno:
                    tipoIcono = "⚖️";
                    break;
                case TipoOrganizacion.Hogar:
                    tipoIcono = "🏠";
                    break;
                default:
                    tipoIcono = "💻";
                    break;
            }

            TreeNode nodo = new TreeNode($"{tipoIcono} {dispositivo.Nombre} [{dispositivo.IP}]");
            nodo.Tag = dispositivo;
            nodo.ToolTipText = $"ID: {dispositivo.IdentificadorTexto}\n" +
                              $"Tipo: {dispositivo.TipoOrganizacion}\n" +
                              $"Dominio: {dispositivo.Dominio}\n" +
                              $"Ruta: {dispositivo.ObtenerRutaCompleta()}\n" +
                              $"FQDN: {dispositivo.ObtenerFQDN()}";

            foreach (var hijo in dispositivo.Hijos)
            {
                nodo.Nodes.Add(CrearNodoArbol(hijo));
            }

            return nodo;
        }

        private void CargarListaDispositivos()
        {
            listViewDispositivos.Items.Clear();
            var dispositivos = dnsManager.ObtenerTodosLosDispositivos();
            foreach (var dispositivo in dispositivos)
            {
                ListViewItem item = new ListViewItem(dispositivo.Nombre);
                item.SubItems.Add(dispositivo.IP);
                item.SubItems.Add(dispositivo.Dominio);
                item.SubItems.Add(dispositivo.TipoOrganizacion.ToString());
                item.Tag = dispositivo;

                // Color de fondo según tipo
                if (dispositivo.TipoOrganizacion == TipoOrganizacion.Universidad)
                    item.BackColor = Color.LightBlue;
                else if (dispositivo.TipoOrganizacion == TipoOrganizacion.Empresa)
                    item.BackColor = Color.LightGreen;

                listViewDispositivos.Items.Add(item);
            }
        }

        private void ActualizarTablaHosts()
        {
            dnsManager.ActualizarTablaHosts();
            dgvTablaHosts.Rows.Clear();

            // Ordenar entradas para mejor visualización
            var entradasOrdenadas = dnsManager.TablaHosts.OrderBy(e => e.Key).ToList();

            foreach (var entry in entradasOrdenadas)
            {
                dgvTablaHosts.Rows.Add(entry.Key, entry.Value);
            }
        }

        private void SincronizarConDHCP()
        {
            if (Form1.DHCPManagerInstance != null)
            {
                dnsManager.LimpiarTodo();

                var clientesDHCP = Form1.DHCPManagerInstance.Clientes;

                // Diccionario para organizar por dominio
                var redesPorDominio = new Dictionary<string, DispositivoDNS>();

                // Primero, identificar todos los dominios únicos
                var dominios = clientesDHCP.Select(c => c.Dominio).Distinct().ToList();

                // Crear redes para cada dominio
                foreach (var dominio in dominios)
                {
                    // Encontrar un cliente de ese dominio para obtener la organización
                    var clienteEjemplo = clientesDHCP.FirstOrDefault(c => c.Dominio == dominio);
                    if (clienteEjemplo != null)
                    {
                        string nombreRed = dominio.Replace(".", "-");
                        string ipRed = ObtenerIPRed(dominio);

                        var nuevaRed = new DispositivoDNS(
                            nombreRed,
                            ipRed,
                            "00:00:00:00:00:00",
                            $"NET-{Guid.NewGuid().ToString().Substring(0, 8)}"
                        );

                        nuevaRed.Dominio = dominio;
                        nuevaRed.TipoOrganizacion = clienteEjemplo.Organizacion == "TecNM" ?
                            TipoOrganizacion.Universidad : TipoOrganizacion.Empresa;

                        dnsManager.Redes.Add(nuevaRed);
                        redesPorDominio[dominio] = nuevaRed;
                    }
                }

                // Agregar todos los clientes a sus respectivas redes
                foreach (var cliente in clientesDHCP.Where(c => c.Activo && !string.IsNullOrEmpty(c.IP) && c.IP != "No asignada"))
                {
                    if (redesPorDominio.ContainsKey(cliente.Dominio))
                    {
                        string identificador = $"DHCP-{cliente.MacAddress.Replace(":", "")}";
                        var dispositivo = new DispositivoDNS(
                            cliente.Hostname,
                            cliente.IP,
                            cliente.MacAddress,
                            identificador
                        );

                        dispositivo.Dominio = cliente.Dominio;
                        dispositivo.TipoOrganizacion = cliente.Organizacion == "TecNM" ?
                            TipoOrganizacion.Universidad : TipoOrganizacion.Empresa;

                        redesPorDominio[cliente.Dominio].AgregarHijo(dispositivo);
                    }
                }

                dnsManager.ActualizarTablaHosts();
                CargarDatos();
            }
        }

        private string ObtenerIPRed(string dominio)
        {
            if (dominio.Contains("google"))
                return "10.0.0.1";
            else if (dominio.Contains("tecnm"))
                return "192.168.1.1";
            else
                return "192.168.0.1";
        }

        private void CrearEstructuraEjemplo()
        {
            dnsManager.LimpiarTodo();

            // ===== GOOGLE.COM (Clase A) =====
            var google = new DispositivoDNS(
                "google",
                "10.0.0.1",
                "00:00:00:00:01:00",
                "ORG-GGL-001"
            );
            google.TipoOrganizacion = TipoOrganizacion.Empresa;
            google.Dominio = "google.com";

            // Departamento de Marketing
            var marketing = new DispositivoDNS(
                "marketing",
                "10.0.1.1",
                "00:00:00:01:00:00",
                "DEPT-MKT-001"
            );
            marketing.TipoOrganizacion = TipoOrganizacion.Empresa;
            marketing.Dominio = "google.com";

            // Dispositivos en Marketing
            var pcAna = new DispositivoDNS(
                "pc-ana",
                "10.0.1.10",
                "00:11:22:33:44:01",
                "DEV-MKT-001"
            );
            pcAna.TipoOrganizacion = TipoOrganizacion.Empresa;
            pcAna.Dominio = "google.com";

            var pcLuis = new DispositivoDNS(
                "pc-luis",
                "10.0.1.11",
                "00:11:22:33:44:02",
                "DEV-MKT-002"
            );
            pcLuis.TipoOrganizacion = TipoOrganizacion.Empresa;
            pcLuis.Dominio = "google.com";

            marketing.AgregarHijo(pcAna);
            marketing.AgregarHijo(pcLuis);

            // Departamento de Compras
            var compras = new DispositivoDNS(
                "compras",
                "10.0.2.1",
                "00:00:00:02:00:00",
                "DEPT-CMP-001"
            );
            compras.TipoOrganizacion = TipoOrganizacion.Empresa;
            compras.Dominio = "google.com";

            // Dispositivos en Compras
            var pcCarlos = new DispositivoDNS(
                "pc-carlos",
                "10.0.2.10",
                "00:11:22:33:44:03",
                "DEV-CMP-001"
            );
            pcCarlos.TipoOrganizacion = TipoOrganizacion.Empresa;
            pcCarlos.Dominio = "google.com";

            var pcMaria = new DispositivoDNS(
                "pc-maria",
                "10.0.2.11",
                "00:11:22:33:44:04",
                "DEV-CMP-002"
            );
            pcMaria.TipoOrganizacion = TipoOrganizacion.Empresa;
            pcMaria.Dominio = "google.com";

            compras.AgregarHijo(pcCarlos);
            compras.AgregarHijo(pcMaria);

            google.AgregarHijo(marketing);
            google.AgregarHijo(compras);

            // ===== TECNM.MX (Clase C) =====
            var tecnm = new DispositivoDNS(
                "tecnm",
                "192.168.1.1",
                "00:00:00:03:00:00",
                "ORG-TNM-001"
            );
            tecnm.TipoOrganizacion = TipoOrganizacion.Universidad;
            tecnm.Dominio = "tecnm.mx";

            // Departamento de ISC
            var isc = new DispositivoDNS(
                "isc",
                "192.168.2.1",
                "00:00:00:04:00:00",
                "DEPT-ISC-001"
            );
            isc.TipoOrganizacion = TipoOrganizacion.Universidad;
            isc.Dominio = "tecnm.mx";

            // Dispositivos en ISC
            var serverISC = new DispositivoDNS(
                "server-isc",
                "192.168.2.10",
                "00:11:22:33:44:05",
                "DEV-ISC-001"
            );
            serverISC.TipoOrganizacion = TipoOrganizacion.Universidad;
            serverISC.Dominio = "tecnm.mx";

            var labISC = new DispositivoDNS(
                "lab-isc",
                "192.168.2.11",
                "00:11:22:33:44:06",
                "DEV-ISC-002"
            );
            labISC.TipoOrganizacion = TipoOrganizacion.Universidad;
            labISC.Dominio = "tecnm.mx";

            isc.AgregarHijo(serverISC);
            isc.AgregarHijo(labISC);

            // Departamento de IGE
            var ige = new DispositivoDNS(
                "ige",
                "192.168.3.1",
                "00:00:00:05:00:00",
                "DEPT-IGE-001"
            );
            ige.TipoOrganizacion = TipoOrganizacion.Universidad;
            ige.Dominio = "tecnm.mx";

            // Dispositivos en IGE
            var serverIGE = new DispositivoDNS(
                "server-ige",
                "192.168.3.10",
                "00:11:22:33:44:07",
                "DEV-IGE-001"
            );
            serverIGE.TipoOrganizacion = TipoOrganizacion.Universidad;
            serverIGE.Dominio = "tecnm.mx";

            var labIGE = new DispositivoDNS(
                "lab-ige",
                "192.168.3.11",
                "00:11:22:33:44:08",
                "DEV-IGE-002"
            );
            labIGE.TipoOrganizacion = TipoOrganizacion.Universidad;
            labIGE.Dominio = "tecnm.mx";

            ige.AgregarHijo(serverIGE);
            ige.AgregarHijo(labIGE);

            tecnm.AgregarHijo(isc);
            tecnm.AgregarHijo(ige);

            // Agregar las redes principales al manager
            dnsManager.Redes.Add(google);
            dnsManager.Redes.Add(tecnm);

            dnsManager.ActualizarTablaHosts();
        }

        private void TreeViewRedes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag is DispositivoDNS dispositivo)
            {
                MostrarDetallesDispositivo(dispositivo);
                btnEliminar.Enabled = true;
            }
        }

        private void ListViewDispositivos_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewDispositivos.SelectedItems.Count > 0)
            {
                var dispositivo = listViewDispositivos.SelectedItems[0].Tag as DispositivoDNS;
                MostrarDetallesDispositivo(dispositivo);
                btnEliminar.Enabled = true;
            }
        }

        private void MostrarDetallesDispositivo(DispositivoDNS dispositivo)
        {
            txtIdentificador.Text = dispositivo.IdentificadorTexto;
            txtNombre.Text = dispositivo.Nombre;
            txtIP.Text = dispositivo.IP;
            txtMac.Text = dispositivo.MacAddress;
            cmbTipoOrganizacion.SelectedItem = dispositivo.TipoOrganizacion.ToString();
            cmbDominio.SelectedItem = dispositivo.Dominio;
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            // Validar campos
            if (string.IsNullOrEmpty(txtNombre.Text))
            {
                MessageBox.Show("El nombre es obligatorio", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(txtIP.Text))
            {
                MessageBox.Show("La IP es obligatoria", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validar IP
            if (!System.Net.IPAddress.TryParse(txtIP.Text, out _))
            {
                MessageBox.Show("IP no válida", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Generar identificador único
            string identificador = $"DEV-{DateTime.Now:yyyyMMddHHmmss}";

            // Crear nuevo dispositivo
            var nuevo = new DispositivoDNS(
                txtNombre.Text,
                txtIP.Text,
                string.IsNullOrEmpty(txtMac.Text) ? "00:00:00:00:00:00" : txtMac.Text,
                identificador
            );

            if (cmbTipoOrganizacion.SelectedItem != null)
            {
                nuevo.TipoOrganizacion = (TipoOrganizacion)Enum.Parse(
                    typeof(TipoOrganizacion),
                    cmbTipoOrganizacion.SelectedItem.ToString()
                );
            }

            if (cmbDominio.SelectedItem != null)
            {
                nuevo.Dominio = cmbDominio.SelectedItem.ToString();
            }

            // Determinar padre (el nodo seleccionado en el árbol)
            DispositivoDNS padre = null;
            if (treeViewRedes.SelectedNode?.Tag is DispositivoDNS selected)
            {
                padre = selected;
            }

            // Agregar a la estructura
            dnsManager.AgregarDispositivo(padre, nuevo);

            // Recargar datos
            CargarDatos();

            MessageBox.Show($"Dispositivo agregado con ID: {identificador}\n" +
                          $"FQDN: {nuevo.ObtenerFQDN()}",
                          "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            DispositivoDNS dispositivo = null;

            if (treeViewRedes.SelectedNode?.Tag is DispositivoDNS dev1)
                dispositivo = dev1;
            else if (listViewDispositivos.SelectedItems.Count > 0)
                dispositivo = listViewDispositivos.SelectedItems[0].Tag as DispositivoDNS;

            if (dispositivo != null)
            {
                string mensaje = dispositivo.Hijos.Count > 0 ?
                    $"¿Eliminar {dispositivo.Nombre} y todos sus {dispositivo.Hijos.Count} hijos?" :
                    $"¿Eliminar {dispositivo.Nombre}?";

                var result = MessageBox.Show(mensaje, "Confirmar Eliminación",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    bool eliminado = dnsManager.EliminarDispositivo(dispositivo);
                    if (eliminado)
                    {
                        CargarDatos();
                        MessageBox.Show("Dispositivo eliminado", "Éxito",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("No se pudo eliminar el dispositivo", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Seleccione un dispositivo para eliminar", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnResolver_Click(object sender, EventArgs e)
        {
            string nombre = txtResolver.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("Ingrese un nombre para resolver", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ip = dnsManager.ResolverNombre(nombre);
            lblResultado.Text = $"IP: {ip}";

            // Mostrar sugerencias si no se encontró
            if (ip == "No encontrado")
            {
                var sugerencias = dnsManager.TablaHosts.Keys
                    .Where(k => k.Contains(nombre))
                    .Take(5)
                    .ToList();

                if (sugerencias.Any())
                {
                    string mensaje = "¿Quizás quisiste decir:\n" + string.Join("\n", sugerencias);
                    MessageBox.Show(mensaje, "Sugerencias",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                // Resaltar en la tabla si existe
                foreach (DataGridViewRow row in dgvTablaHosts.Rows)
                {
                    if (row.Cells[0].Value?.ToString().Equals(nombre, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        row.Selected = true;
                        dgvTablaHosts.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
        }

        private void BtnSincronizarDHCP_Click(object sender, EventArgs e)
        {
            SincronizarConDHCP();
            int total = dnsManager.ContarDispositivos();
            MessageBox.Show($"Sincronización completada.\n{total} dispositivos en la estructura DNS.",
                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnLimpiarTodo_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("¿Limpiar todas las redes y dispositivos?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                dnsManager.LimpiarTodo();
                CargarDatos();
                MessageBox.Show("Todos los datos han sido eliminados", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCrearEstructura_Click(object sender, EventArgs e)
        {
            CrearEstructuraEjemplo();
            CargarDatos();
            MessageBox.Show("Estructura de ejemplo creada exitosamente.\n\n" +
                "Google.com:\n" +
                "  - marketing (pc-ana, pc-luis)\n" +
                "  - compras (pc-carlos, pc-maria)\n\n" +
                "Tecnm.mx:\n" +
                "  - isc (server-isc, lab-isc)\n" +
                "  - ige (server-ige, lab-ige)",
                "Estructura Creada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Eventos de DNS para notificar a Form1
        // Eventos de DNS para notificar a Form1
        // Eventos de DNS para notificar a Form1
        private void OnDispositivoAgregado(DispositivoDNS dispositivo)
        {
            // Buscar Form1 entre los formularios abiertos
            foreach (Form form in Application.OpenForms)
            {
                if (form is Form1 form1)
                {
                    // Llamar al método público que crearemos en Form1
                    form1.NotificarDispositivoDNSAgregado(dispositivo);
                    break;
                }
            }
        }

        private void OnDispositivoEliminado(DispositivoDNS dispositivo)
        {
            // Buscar Form1 entre los formularios abiertos
            foreach (Form form in Application.OpenForms)
            {
                if (form is Form1 form1)
                {
                    // Llamar al método público que crearemos en Form1
                    form1.NotificarDispositivoDNSEliminado(dispositivo);
                    break;
                }
            }
        }

        private void OnIPCambio(DispositivoDNS dispositivo, string ipAnterior, string ipNueva)
        {
            // Notificar cambio de IP si es necesario
        }

        // Eventos de DHCP
        private void OnClienteDHCPAgregado(ClienteDHCP cliente)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnClienteDHCPAgregado(cliente)));
                return;
            }

            if (this.Visible)
            {
                SincronizarConDHCP();
                MessageBox.Show($"Cliente {cliente.Hostname} agregado en DHCP.\nSincronización completada.",
                    "DNS Actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnClienteDHCPEliminado(ClienteDHCP cliente)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnClienteDHCPEliminado(cliente)));
                return;
            }

            if (this.Visible)
            {
                SincronizarConDHCP();
            }
        }

        private void OnIPCambioDHCP(ClienteDHCP cliente, string ipAnterior, string ipNueva)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnIPCambioDHCP(cliente, ipAnterior, ipNueva)));
                return;
            }

            // Buscar el dispositivo por MAC
            string identificador = $"DHCP-{cliente.MacAddress.Replace(":", "")}";
            var dispositivo = dnsManager.BuscarPorIdentificador(identificador);

            if (dispositivo != null)
            {
                dispositivo.IP = ipNueva;
                dnsManager.ActualizarTablaHosts();
                CargarDatos();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Desuscribirse de eventos
            if (Form1.DHCPManagerInstance != null)
            {
                Form1.DHCPManagerInstance.ClienteAgregado -= OnClienteDHCPAgregado;
                Form1.DHCPManagerInstance.ClienteEliminado -= OnClienteDHCPEliminado;
                Form1.DHCPManagerInstance.IPCambio -= OnIPCambioDHCP;
            }
            base.OnFormClosing(e);
        }
    }
}