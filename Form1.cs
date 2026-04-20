using System;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public partial class Form1 : Form
    {
        private Button btnAbrirCarpetaLog;
        private MenuStrip menuStrip;
        private ToolStripMenuItem dhcpMenuItem;
        private ToolStripMenuItem dnsMenuItem;
        private ToolStripMenuItem httpMenuItem;
        private ToolStripMenuItem ftpMenuItem;

        private ListBox listBoxClientes;
        private Panel panelInfoCliente;
        private DHCPManager dhcpManager;
        private ToolStripMenuItem sshMenuItem;   // ◄ AGREGAR ►
        // Referencia estática para compartir con DNS
        public static DHCPManager DHCPManagerInstance { get; private set; }

        // Eventos estáticos para comunicación con DNS
        public static event Action<DispositivoDNS> DispositivoDNSAgregado;
        public static event Action<DispositivoDNS> DispositivoDNSEliminado;

        // Controles para mostrar información del cliente
        private TextBox txtMac;
        private TextBox txtIP;
        private TextBox txtHostname;
        private CheckBox chkActivo;
        private GroupBox groupTrafico;
        private TrackBar trackTrafico;
        private Label lblTrafico;
        private Button btnIniciarTrafico;
        private Button btnDetenerTrafico;
        private NumericUpDown nudTiempoInactividad;
        private Label lblMascara;

        // Botones para activar/desactivar
        private Button btnActivarCliente;
        private Button btnDesactivarCliente;

        // Botón para agregar cliente
        private Button btnAgregarCliente;
        private Label lblTotalClientes;

        // Controles para editar IP manualmente
        private Button btnEditarIP;
        private Button btnGuardarIP;
        private Button btnCancelarEdicion;
        private ComboBox cmbRedBase;
        private NumericUpDown nudOcteto4;
        private Label lblInfoRed;
        private bool modoEdicionIP = false;
        private string ipOriginalEnEdicion;

        public Form1()
        {
            InitializeComponent();

            // PRIMERO inicializar el DHCPManager
            dhcpManager = new DHCPManager();
            DHCPManagerInstance = dhcpManager;

            // Suscribir eventos de DHCP
            dhcpManager.ClienteDesactivado += OnClienteDesactivado;
            dhcpManager.ClienteAgregado += OnClienteAgregado;
            dhcpManager.ClienteEliminado += OnClienteEliminado;
            dhcpManager.IPCambio += OnIPCambio;

            // Suscribir eventos estáticos de DNS (para comunicación bidireccional)
            DispositivoDNSAgregado += OnDispositivoDNSAgregado;
            DispositivoDNSEliminado += OnDispositivoDNSEliminado;

            // DESPUÉS inicializar la interfaz
            InicializarMenu();
            InicializarControles();

            // Crear clientes con la misma estructura que DNS
            CrearClientesConEstructuraDNS();

            // Iniciar medición de tráfico para todos los clientes
            foreach (var cliente in dhcpManager.Clientes)
            {
                int valorInicial = 20 + (cliente.GetHashCode() % 50);
                cliente.IniciarMedicionTrafico(Math.Max(20, Math.Min(70, valorInicial)));
            }

            ActualizarListBoxClientes();
            ActualizarTotalClientes();
        }

        private void CrearClientesConEstructuraDNS()
        {
            // ===== GOOGLE.COM (Clase A) =====
            dhcpManager.AgregarClienteEjemplo("google", "10.0.0.1", "00:00:00:00:01:00", "google.com", "Google", "10.0.0");
            dhcpManager.AgregarClienteEjemplo("marketing", "10.0.1.1", "00:00:00:01:00:00", "google.com", "Google", "10.0.0");
            dhcpManager.AgregarClienteEjemplo("pc-ana", "10.0.1.10", "00:11:22:33:44:01", "google.com", "Google", "10.0.0");
            dhcpManager.AgregarClienteEjemplo("pc-luis", "10.0.1.11", "00:11:22:33:44:02", "google.com", "Google", "10.0.0");
            dhcpManager.AgregarClienteEjemplo("compras", "10.0.2.1", "00:00:00:02:00:00", "google.com", "Google", "10.0.0");
            dhcpManager.AgregarClienteEjemplo("pc-carlos", "10.0.2.10", "00:11:22:33:44:03", "google.com", "Google", "10.0.0");
            dhcpManager.AgregarClienteEjemplo("pc-maria", "10.0.2.11", "00:11:22:33:44:04", "google.com", "Google", "10.0.0");

            // ===== TECNM.MX (Clase C) =====
            dhcpManager.AgregarClienteEjemplo("tecnm", "192.168.1.1", "00:00:00:03:00:00", "tecnm.mx", "TecNM", "192.168.1");
            dhcpManager.AgregarClienteEjemplo("isc", "192.168.2.1", "00:00:00:04:00:00", "tecnm.mx", "TecNM", "192.168.2");
            dhcpManager.AgregarClienteEjemplo("server-isc", "192.168.2.10", "00:11:22:33:44:05", "tecnm.mx", "TecNM", "192.168.2");
            dhcpManager.AgregarClienteEjemplo("lab-isc", "192.168.2.11", "00:11:22:33:44:06", "tecnm.mx", "TecNM", "192.168.2");
            dhcpManager.AgregarClienteEjemplo("ige", "192.168.3.1", "00:00:00:05:00:00", "tecnm.mx", "TecNM", "192.168.3");
            dhcpManager.AgregarClienteEjemplo("server-ige", "192.168.3.10", "00:11:22:33:44:07", "tecnm.mx", "TecNM", "192.168.3");
            dhcpManager.AgregarClienteEjemplo("lab-ige", "192.168.3.11", "00:11:22:33:44:08", "tecnm.mx", "TecNM", "192.168.3");
        }

        private void InicializarMenu()
        {
            menuStrip = new MenuStrip();

            dhcpMenuItem = new ToolStripMenuItem("DHCP");
            dhcpMenuItem.Click += (s, e) => MostrarPanelDHCP();

            dnsMenuItem = new ToolStripMenuItem("DNS");
            dnsMenuItem.Click += (s, e) => MostrarPanelDNS();

            httpMenuItem = new ToolStripMenuItem("HTTP");
            httpMenuItem.Click += (s, e) => MostrarPanelHTTP();

            ftpMenuItem = new ToolStripMenuItem("FTP");
            ftpMenuItem.Click += (s, e) => MostrarPanelFTP();

            sshMenuItem = new ToolStripMenuItem("SSH");
            sshMenuItem.Click += (s, e) => MostrarPanelSSH();

            menuStrip.Items.AddRange(new ToolStripItem[]            // ◄ MODIFICAR ►
             {
                dhcpMenuItem,
                dnsMenuItem,
                sshMenuItem,
                httpMenuItem,
                ftpMenuItem
             });

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }
        private void MostrarPanelHTTP()
        {
            FormHTTP formHTTP = new FormHTTP();
            formHTTP.ShowDialog();
        }
        private void InicializarControles()
        {
            this.Size = new Size(950, 650);
            this.Text = "Simulador de Redes - DHCP";

            // Panel superior para controles adicionales
            Panel panelSuperior = new Panel
            {
                Location = new Point(12, 40),
                Size = new Size(900, 50),
                BorderStyle = BorderStyle.None
            };

            // Botón para agregar cliente
            btnAgregarCliente = new Button
            {
                Text = "➕ Agregar Nuevo Cliente",
                Location = new Point(0, 10),
                Size = new Size(180, 30),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnAgregarCliente.Click += BtnAgregarCliente_Click;

            // Label para mostrar total de clientes
            lblTotalClientes = new Label
            {
                Text = "Total clientes: 14",
                Location = new Point(200, 15),
                Size = new Size(150, 25),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnAbrirCarpetaLog = new Button
            {
                Text = "📂 Logs",
                Location = new Point(360, 10),  // ← Posición X=360, Y=10
                Size = new Size(80, 30),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnAbrirCarpetaLog.Click += (s, e) => AbrirCarpetaLog();

            panelSuperior.Controls.AddRange(new Control[]
            {
        btnAgregarCliente,
        lblTotalClientes,
        btnAbrirCarpetaLog
            });

            // ListBox de clientes
            listBoxClientes = new ListBox
            {
                Location = new Point(12, 130),
                Size = new Size(250, 400),
                DisplayMember = "Hostname"
            };
            listBoxClientes.SelectedIndexChanged += ListBoxClientes_SelectedIndexChanged;

            // Panel de información del cliente
            panelInfoCliente = new Panel
            {
                Location = new Point(270, 130),
                Size = new Size(650, 450),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Controles para mostrar información
            int yPos = 20;
            int spacing = 35;

            // MAC Address
            Label lblMac = new Label { Text = "MAC:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            txtMac = new TextBox { Location = new Point(100, yPos), Size = new Size(200, 25), ReadOnly = true };
            panelInfoCliente.Controls.AddRange(new Control[] { lblMac, txtMac });

            // IP - Con edición
            yPos += spacing;
            Label lblIP = new Label { Text = "IP:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            txtIP = new TextBox { Location = new Point(100, yPos), Size = new Size(150, 25), ReadOnly = true };

            // Botones para editar IP
            btnEditarIP = new Button
            {
                Text = "✏️ Editar IP",
                Location = new Point(260, yPos),
                Size = new Size(90, 25),
                BackColor = Color.LightYellow
            };
            btnEditarIP.Click += BtnEditarIP_Click;

            btnGuardarIP = new Button
            {
                Text = "💾 Guardar",
                Location = new Point(360, yPos),
                Size = new Size(80, 25),
                BackColor = Color.LightGreen,
                Visible = false
            };
            btnGuardarIP.Click += BtnGuardarIP_Click;

            btnCancelarEdicion = new Button
            {
                Text = "❌ Cancelar",
                Location = new Point(450, yPos),
                Size = new Size(80, 25),
                BackColor = Color.LightCoral,
                Visible = false
            };
            btnCancelarEdicion.Click += BtnCancelarEdicion_Click;

            // Control para editar el último octeto
            nudOcteto4 = new NumericUpDown
            {
                Location = new Point(100, yPos),
                Size = new Size(60, 25),
                Minimum = 2,
                Maximum = 254,
                Visible = false
            };

            panelInfoCliente.Controls.AddRange(new Control[]
            {
                lblIP, txtIP, btnEditarIP, btnGuardarIP, btnCancelarEdicion, nudOcteto4
            });

            // Hostname
            yPos += spacing;
            Label lblHostname = new Label { Text = "HOSTNAME:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            txtHostname = new TextBox { Location = new Point(100, yPos), Size = new Size(200, 25), ReadOnly = true };

            // Label para mostrar organización
            Label lblOrganizacion = new Label
            {
                Text = "",
                Location = new Point(310, yPos),
                Size = new Size(150, 25),
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            panelInfoCliente.Controls.AddRange(new Control[] { lblHostname, txtHostname, lblOrganizacion });

            // Activo
            yPos += spacing;
            Label lblActivo = new Label { Text = "Activo:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            chkActivo = new CheckBox { Location = new Point(100, yPos), Size = new Size(30, 25), Enabled = false };
            panelInfoCliente.Controls.AddRange(new Control[] { lblActivo, chkActivo });

            // Grupo de tráfico
            yPos += spacing + 10;
            groupTrafico = new GroupBox
            {
                Text = "Medición de Tráfico",
                Location = new Point(10, yPos),
                Size = new Size(350, 120)
            };

            trackTrafico = new TrackBar
            {
                Location = new Point(10, 25),
                Size = new Size(250, 45),
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10
            };
            trackTrafico.Scroll += TrackTrafico_Scroll;

            lblTrafico = new Label
            {
                Text = "0 Mbps",
                Location = new Point(270, 30),
                Size = new Size(60, 25)
            };

            btnIniciarTrafico = new Button
            {
                Text = "Iniciar Medición",
                Location = new Point(10, 70),
                Size = new Size(100, 30)
            };
            btnIniciarTrafico.Click += BtnIniciarTrafico_Click;

            btnDetenerTrafico = new Button
            {
                Text = "Detener",
                Location = new Point(120, 70),
                Size = new Size(80, 30),
                Enabled = false
            };
            btnDetenerTrafico.Click += BtnDetenerTrafico_Click;

            groupTrafico.Controls.AddRange(new Control[] { trackTrafico, lblTrafico, btnIniciarTrafico, btnDetenerTrafico });
            panelInfoCliente.Controls.Add(groupTrafico);

            // Control para tiempo de inactividad
            yPos += 130;
            Label lblTiempoInactividad = new Label
            {
                Text = "Tiempo inactividad (seg):",
                Location = new Point(10, yPos),
                Size = new Size(150, 25)
            };

            nudTiempoInactividad = new NumericUpDown
            {
                Location = new Point(160, yPos),
                Size = new Size(60, 25),
                Minimum = 5,
                Maximum = 60,
                Value = 10
            };
            nudTiempoInactividad.ValueChanged += (s, e) =>
                dhcpManager.TiempoInactividadLimite = (int)nudTiempoInactividad.Value;

            panelInfoCliente.Controls.AddRange(new Control[] { lblTiempoInactividad, nudTiempoInactividad });

            // Botones para activar/desactivar
            yPos += spacing;

            btnActivarCliente = new Button
            {
                Text = "Activar Cliente",
                Location = new Point(10, yPos),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen
            };
            btnActivarCliente.Click += BtnActivarCliente_Click;
            panelInfoCliente.Controls.Add(btnActivarCliente);

            btnDesactivarCliente = new Button
            {
                Text = "Desactivar Cliente",
                Location = new Point(140, yPos),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral
            };
            btnDesactivarCliente.Click += BtnDesactivarCliente_Click;
            panelInfoCliente.Controls.Add(btnDesactivarCliente);

            // Mostrar máscara de red calculada
            yPos += spacing;
            lblMascara = new Label
            {
                Text = $"Máscara de red: {CalcularMascaraPorIP("192.168.1.1")}",
                Location = new Point(10, yPos),
                Size = new Size(400, 30),
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = Color.Black
            };
            panelInfoCliente.Controls.Add(lblMascara);

            this.Controls.Add(panelSuperior);
            this.Controls.Add(listBoxClientes);
            this.Controls.Add(panelInfoCliente);


        }

        private void MostrarPanelDHCP()
        {
            listBoxClientes.Visible = true;
            panelInfoCliente.Visible = true;
            this.Text = "Simulador de Redes - DHCP";
        }

        private void MostrarPanelDNS()
        {
            FormDNS formDNS = new FormDNS();
            formDNS.ShowDialog();
        }

        private void MostrarPanelSSH()                              // ◄ AGREGAR ►
        {
            FormSSH formSSH = new FormSSH(dhcpManager);
            formSSH.ShowDialog();
        }
        private void MostrarPanelFTP()
        {
            FormFTP formFTP = new FormFTP(dhcpManager);
            formFTP.ShowDialog();
        }
        private void ActualizarListBoxClientes()
        {
            listBoxClientes.DataSource = null;
            listBoxClientes.DataSource = dhcpManager.Clientes;
        }

        private void ActualizarTotalClientes()
        {
            lblTotalClientes.Text = $"Total clientes: {dhcpManager.Clientes.Count}";
        }

        private void ListBoxClientes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                if (modoEdicionIP)
                {
                    CancelarEdicionIP();
                }
                MostrarInformacionCliente(cliente);
            }
        }

        private void MostrarInformacionCliente(ClienteDHCP cliente)
        {
            txtMac.Text = cliente.MacAddress;
            txtIP.Text = cliente.IP ?? "No asignada";
            txtHostname.Text = cliente.Hostname;
            chkActivo.Checked = cliente.Activo;

            // Mostrar organización
            var lblOrganizacion = panelInfoCliente.Controls.OfType<Label>()
                .FirstOrDefault(l => l.Location.Y == 125 && l.Location.X == 310);
            if (lblOrganizacion != null)
            {
                lblOrganizacion.Text = $"[{cliente.Organizacion}]";
                if (cliente.Organizacion == "TecNM")
                    lblOrganizacion.ForeColor = Color.Green;
                else if (cliente.Organizacion == "Google")
                    lblOrganizacion.ForeColor = Color.Blue;
            }

            if (!string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
            {
                lblMascara.Text = $"Máscara de red: {CalcularMascaraPorIP(cliente.IP)}";
            }
            else
            {
                lblMascara.Text = $"Máscara de red: {CalcularMascaraPorIP("192.168.1.1")}";
            }

            btnActivarCliente.Enabled = !cliente.Activo;
            btnDesactivarCliente.Enabled = cliente.Activo;
            btnEditarIP.Enabled = cliente.Activo && !string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada";

            int traficoValor = cliente.Trafico;
            if (traficoValor < trackTrafico.Minimum)
                traficoValor = trackTrafico.Minimum;
            else if (traficoValor > trackTrafico.Maximum)
                traficoValor = trackTrafico.Maximum;

            trackTrafico.Value = traficoValor;
            lblTrafico.Text = $"{cliente.Trafico} Mbps";

            btnIniciarTrafico.Enabled = cliente.Activo && !cliente.MedicionActiva;
            btnDetenerTrafico.Enabled = cliente.MedicionActiva;
        }

        private string CalcularMascaraPorIP(string ip)
        {
            try
            {
                if (IPAddress.TryParse(ip, out IPAddress ipAddr))
                {
                    byte[] bytes = ipAddr.GetAddressBytes();
                    int primerOcteto = bytes[0];

                    if (primerOcteto >= 1 && primerOcteto <= 126)
                        return "255.0.0.0 (Clase A)";
                    else if (primerOcteto >= 128 && primerOcteto <= 191)
                        return "255.255.0.0 (Clase B)";
                    else if (primerOcteto >= 192 && primerOcteto <= 223)
                        return "255.255.255.0 (Clase C)";
                    else
                        return "255.255.255.0 (Estándar)";
                }
            }
            catch
            {
                // Ignorar errores
            }
            return "255.255.255.0 (Estándar)";
        }

        private void CmbRedBase_SelectedIndexChanged(object sender, EventArgs e)
        {
            MessageBox.Show("No se puede cambiar la red base porque hay múltiples redes activas.\n" +
                "TecNM usa Clase C (192.168.x.x) y Google usa Clase A (10.0.x.x)",
                "Operación no disponible", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnEditarIP_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente && !string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
            {
                modoEdicionIP = true;
                ipOriginalEnEdicion = cliente.IP;

                txtIP.Visible = false;
                btnEditarIP.Visible = false;

                string[] partes = cliente.IP.Split('.');
                if (partes.Length == 4)
                {
                    nudOcteto4.Value = int.Parse(partes[3]);
                    nudOcteto4.Visible = true;
                }

                btnGuardarIP.Visible = true;
                btnCancelarEdicion.Visible = true;
            }
        }

        private void BtnGuardarIP_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                string redBase = cliente.RedBase;
                string nuevaIP = $"{redBase}.{nudOcteto4.Value}";

                var clienteExistente = dhcpManager.Clientes
                    .FirstOrDefault(c => c.IP == nuevaIP && c != cliente);

                if (clienteExistente != null)
                {
                    MessageBox.Show($"La IP {nuevaIP} ya está siendo usada por {clienteExistente.Hostname}.\nPor favor elige otra IP.",
                        "IP Duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (dhcpManager.IPsReservadas.Contains(nuevaIP) && !cliente.TieneReserva)
                {
                    var result = MessageBox.Show($"La IP {nuevaIP} está prevenida/reservada para otro cliente.\n¿Deseas usarla de todas formas?",
                        "IP Reservada", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                        return;
                }

                string ipAnterior = cliente.IP;
                dhcpManager.LiberarIP(ipAnterior, cliente);
                cliente.IP = nuevaIP;
                dhcpManager.IPsOcupadas.Add(nuevaIP);

                FinalizarEdicionIP();

                MostrarInformacionCliente(cliente);
                ActualizarListBoxClientes();

                // Usar el evento en lugar del método directo
                dhcpManager.NotificarIPCambio(cliente, ipAnterior, nuevaIP);

                MessageBox.Show($"IP cambiada de {ipAnterior} a {nuevaIP}", "IP Actualizada",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnCancelarEdicion_Click(object sender, EventArgs e)
        {
            CancelarEdicionIP();
        }

        private void CancelarEdicionIP()
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                cliente.IP = ipOriginalEnEdicion;
                FinalizarEdicionIP();
                MostrarInformacionCliente(cliente);
            }
        }

        private void FinalizarEdicionIP()
        {
            modoEdicionIP = false;
            txtIP.Visible = true;
            btnEditarIP.Visible = true;
            nudOcteto4.Visible = false;
            btnGuardarIP.Visible = false;
            btnCancelarEdicion.Visible = false;
        }

        private void BtnAgregarCliente_Click(object sender, EventArgs e)
        {
            using (Form dialogo = new Form())
            {
                dialogo.Text = "Agregar Nuevo Cliente";
                dialogo.Size = new Size(300, 300);
                dialogo.StartPosition = FormStartPosition.CenterParent;
                dialogo.FormBorderStyle = FormBorderStyle.FixedDialog;

                Label lblNombre = new Label { Text = "Nombre:", Location = new Point(10, 20), Size = new Size(80, 25) };
                TextBox txtNombre = new TextBox { Location = new Point(100, 20), Size = new Size(150, 25) };

                Label lblDominio = new Label { Text = "Dominio:", Location = new Point(10, 60), Size = new Size(80, 25) };
                ComboBox cmbDominio = new ComboBox
                {
                    Location = new Point(100, 60),
                    Size = new Size(150, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbDominio.Items.AddRange(new string[] { "google.com", "tecnm.mx", "local" });
                cmbDominio.SelectedIndex = 0;

                Label lblOrganizacion = new Label { Text = "Org:", Location = new Point(10, 100), Size = new Size(80, 25) };
                ComboBox cmbOrganizacion = new ComboBox
                {
                    Location = new Point(100, 100),
                    Size = new Size(150, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbOrganizacion.Items.AddRange(new string[] { "Google", "TecNM", "Otro" });
                cmbOrganizacion.SelectedIndex = 0;

                cmbDominio.SelectedIndexChanged += (s, ev) =>
                {
                    if (cmbDominio.SelectedItem.ToString() == "google.com")
                        cmbOrganizacion.SelectedItem = "Google";
                    else if (cmbDominio.SelectedItem.ToString() == "tecnm.mx")
                        cmbOrganizacion.SelectedItem = "TecNM";
                    else
                        cmbOrganizacion.SelectedItem = "Otro";
                };

                Button btnAceptar = new Button
                {
                    Text = "Aceptar",
                    Location = new Point(50, 200),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.OK
                };

                Button btnCancelar = new Button
                {
                    Text = "Cancelar",
                    Location = new Point(150, 200),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.Cancel
                };

                dialogo.Controls.AddRange(new Control[] { lblNombre, txtNombre, lblDominio, cmbDominio,
                    lblOrganizacion, cmbOrganizacion, btnAceptar, btnCancelar });

                if (dialogo.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(txtNombre.Text))
                {
                    string dominio = cmbDominio.SelectedItem.ToString();
                    string organizacion = cmbOrganizacion.SelectedItem.ToString();

                    string redBase;
                    if (dominio == "google.com")
                        redBase = "10.0.0";
                    else if (dominio == "tecnm.mx")
                        redBase = "192.168.1";
                    else
                        redBase = "192.168.0";

                    int ultimoOcteto = 20;
                    string ip;
                    do
                    {
                        ip = $"{redBase}.{ultimoOcteto}";
                        ultimoOcteto++;
                    } while (dhcpManager.IPsOcupadas.Contains(ip) && ultimoOcteto < 255);

                    string mac = $"00:1A:2B:3C:{dhcpManager.Clientes.Count:D2}:{dhcpManager.Clientes.Count:D2}";

                    dhcpManager.AgregarClienteEjemplo(txtNombre.Text, ip, mac, dominio, organizacion, redBase);

                    var nuevoCliente = dhcpManager.Clientes.Last();
                    nuevoCliente.IniciarMedicionTrafico(30);

                    ActualizarListBoxClientes();
                    ActualizarTotalClientes();

                    MessageBox.Show($"Cliente {txtNombre.Text} agregado con IP {ip}",
                        "Cliente Agregado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void TrackTrafico_Scroll(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente && cliente.MedicionActiva)
            {
                cliente.Trafico = trackTrafico.Value;
                lblTrafico.Text = $"{trackTrafico.Value} Mbps";
            }
        }

        private void BtnIniciarTrafico_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                cliente.IniciarMedicionTrafico(trackTrafico.Value);
                btnIniciarTrafico.Enabled = false;
                btnDetenerTrafico.Enabled = true;
            }
        }

        private void BtnDetenerTrafico_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                cliente.DetenerMedicionTrafico();
                trackTrafico.Value = 0;
                lblTrafico.Text = "0 Mbps";
                btnIniciarTrafico.Enabled = true;
                btnDetenerTrafico.Enabled = false;
            }
        }

        private void BtnActivarCliente_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                string ipAnterior = cliente.IP;
                cliente.Activar();

                MostrarInformacionCliente(cliente);
                ActualizarListBoxClientes();

                if (ipAnterior == "No asignada" && cliente.IP != "No asignada")
                {
                    MessageBox.Show($"Cliente {cliente.Hostname} activado.\nIP asignada: {cliente.IP}",
                        "Cliente Activado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Cliente {cliente.Hostname} activado",
                        "Cliente Activado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnDesactivarCliente_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                string ipLiberada = cliente.IP;
                cliente.Desactivar();

                MostrarInformacionCliente(cliente);
                ActualizarListBoxClientes();

                if (!string.IsNullOrEmpty(ipLiberada) && ipLiberada != "No asignada" && !cliente.TieneReserva)
                {
                    MessageBox.Show($"Cliente {cliente.Hostname} desactivado.\nIP {ipLiberada} liberada.",
                        "Cliente Desactivado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Cliente {cliente.Hostname} desactivado manualmente",
                        "Cliente Desactivado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Manejadores de eventos de DHCP
        private void OnClienteDesactivado(ClienteDHCP cliente)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnClienteDesactivado(cliente)));
                return;
            }

            if (listBoxClientes.SelectedItem == cliente)
            {
                MostrarInformacionCliente(cliente);
            }

            ActualizarListBoxClientes();
        }

        private void OnClienteAgregado(ClienteDHCP cliente)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnClienteAgregado(cliente)));
                return;
            }

            ActualizarListBoxClientes();
            ActualizarTotalClientes();
        }

        private void OnClienteEliminado(ClienteDHCP cliente)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnClienteEliminado(cliente)));
                return;
            }

            ActualizarListBoxClientes();
            ActualizarTotalClientes();
        }

        private void OnIPCambio(ClienteDHCP cliente, string ipAnterior, string ipNueva)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnIPCambio(cliente, ipAnterior, ipNueva)));
                return;
            }

            if (listBoxClientes.SelectedItem == cliente)
            {
                MostrarInformacionCliente(cliente);
            }
        }

        // Manejadores de eventos de DNS
        private void OnDispositivoDNSAgregado(DispositivoDNS dispositivo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnDispositivoDNSAgregado(dispositivo)));
                return;
            }

            // Verificar si ya existe en DHCP
            var existe = dhcpManager.Clientes.Any(c => c.Hostname == dispositivo.Nombre && c.Dominio == dispositivo.Dominio);

            if (!existe && !string.IsNullOrEmpty(dispositivo.IP))
            {
                string redBase = "192.168.1";
                if (dispositivo.Dominio.Contains("google"))
                    redBase = "10.0.0";
                else if (dispositivo.Dominio.Contains("tecnm"))
                    redBase = "192.168.1";

                dhcpManager.AgregarCliente(
                    dispositivo.Nombre,
                    dispositivo.IP,
                    dispositivo.MacAddress,
                    dispositivo.Dominio,
                    dispositivo.TipoOrganizacion.ToString(),
                    redBase
                );

                ActualizarListBoxClientes();
                ActualizarTotalClientes();

                MessageBox.Show($"Cliente {dispositivo.Nombre} agregado desde DNS.",
                    "Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnDispositivoDNSEliminado(DispositivoDNS dispositivo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnDispositivoDNSEliminado(dispositivo)));
                return;
            }

            bool eliminado = dhcpManager.EliminarClientePorHostname(dispositivo.Nombre);

            if (eliminado)
            {
                ActualizarListBoxClientes();
                ActualizarTotalClientes();

                MessageBox.Show($"Cliente {dispositivo.Nombre} eliminado desde DNS.",
                    "Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            dhcpManager?.DetenerTodosLosTimers();
            base.OnFormClosing(e);
        }
        public void NotificarDispositivoDNSAgregado(DispositivoDNS dispositivo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => NotificarDispositivoDNSAgregado(dispositivo)));
                return;
            }

            // Aquí va la lógica para agregar el dispositivo a DHCP
            var existe = dhcpManager.Clientes.Any(c => c.Hostname == dispositivo.Nombre && c.Dominio == dispositivo.Dominio);

            if (!existe && !string.IsNullOrEmpty(dispositivo.IP))
            {
                string redBase = "192.168.1";
                if (dispositivo.Dominio.Contains("google"))
                    redBase = "10.0.0";
                else if (dispositivo.Dominio.Contains("tecnm"))
                    redBase = "192.168.1";

                dhcpManager.AgregarCliente(
                    dispositivo.Nombre,
                    dispositivo.IP,
                    dispositivo.MacAddress,
                    dispositivo.Dominio,
                    dispositivo.TipoOrganizacion.ToString(),
                    redBase
                );

                ActualizarListBoxClientes();
                ActualizarTotalClientes();

                MessageBox.Show($"Cliente {dispositivo.Nombre} agregado desde DNS.",
                    "Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void NotificarDispositivoDNSEliminado(DispositivoDNS dispositivo)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => NotificarDispositivoDNSEliminado(dispositivo)));
                return;
            }

            bool eliminado = dhcpManager.EliminarClientePorHostname(dispositivo.Nombre);

            if (eliminado)
            {
                ActualizarListBoxClientes();
                ActualizarTotalClientes();

                MessageBox.Show($"Cliente {dispositivo.Nombre} eliminado desde DNS.",
                    "Sincronización", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void AbrirCarpetaLog()
        {
            try
            {
                string rutaLog = Path.Combine(Application.StartupPath, "simulador_log.txt");
                string carpeta = Path.GetDirectoryName(rutaLog);

                if (Directory.Exists(carpeta))
                {
                    // Abrir la carpeta en el explorador de Windows
                    System.Diagnostics.Process.Start("explorer.exe", carpeta);

                    // También podemos hacer un log de esta acción
                    // Asumiendo que tienes un sistema de logging
                        Logger.Log("Carpeta de logs abierta en explorador");
                }
                else
                {
                    MessageBox.Show("No se pudo encontrar la carpeta de logs.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir carpeta: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}