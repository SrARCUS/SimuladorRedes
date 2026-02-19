using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SimuladorRedes
{
    public partial class Form1 : Form
    {
        private MenuStrip menuStrip;
        private ToolStripMenuItem dhcpMenuItem;
        private ToolStripMenuItem dnsMenuItem;
        private ToolStripMenuItem httpMenuItem;
        private ToolStripMenuItem ftpMenuItem;

        private ListBox listBoxClientes;
        private Panel panelInfoCliente;
        private DHCPManager dhcpManager;

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
        private Button btnReservarIP;
        private NumericUpDown nudTiempoInactividad;
        private Button btnPrevenirIP;
        private Label lblMascara;

        public Form1()
        {
            InitializeComponent();

            // PRIMERO inicializar el DHCPManager
            dhcpManager = new DHCPManager();
            dhcpManager.ClienteDesactivado += OnClienteDesactivado;

            // DESPUÉS inicializar la interfaz
            InicializarMenu();
            InicializarControles();

            // Agregar clientes de ejemplo
            dhcpManager.AgregarClienteEjemplo(1);
            dhcpManager.AgregarClienteEjemplo(2);

            ActualizarListBoxClientes();
        }

        private void InicializarMenu()
        {
            menuStrip = new MenuStrip();

            dhcpMenuItem = new ToolStripMenuItem("DHCP");
            dhcpMenuItem.Click += (s, e) => MostrarPanelDHCP();

            dnsMenuItem = new ToolStripMenuItem("DNS");
            httpMenuItem = new ToolStripMenuItem("HTTP");
            ftpMenuItem = new ToolStripMenuItem("FTP");

            menuStrip.Items.AddRange(new ToolStripItem[]
            {
                dhcpMenuItem,
                dnsMenuItem,
                httpMenuItem,
                ftpMenuItem
            });

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void InicializarControles()
        {
            this.Size = new Size(900, 600);
            this.Text = "Simulador de Redes - DHCP";

            // ListBox de clientes
            listBoxClientes = new ListBox
            {
                Location = new Point(12, 40),
                Size = new Size(250, 400),
                DisplayMember = "Hostname"
            };
            listBoxClientes.SelectedIndexChanged += ListBoxClientes_SelectedIndexChanged;

            // Panel de información del cliente
            panelInfoCliente = new Panel
            {
                Location = new Point(270, 40),
                Size = new Size(600, 400),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Controles para mostrar información
            int yPos = 20;
            int spacing = 35;

            // MAC Address
            Label lblMac = new Label { Text = "MAC:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            txtMac = new TextBox { Location = new Point(100, yPos), Size = new Size(200, 25), ReadOnly = true };
            panelInfoCliente.Controls.AddRange(new Control[] { lblMac, txtMac });

            // IP
            yPos += spacing;
            Label lblIP = new Label { Text = "IP:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            txtIP = new TextBox { Location = new Point(100, yPos), Size = new Size(200, 25), ReadOnly = true };
            panelInfoCliente.Controls.AddRange(new Control[] { lblIP, txtIP });

            // Hostname
            yPos += spacing;
            Label lblHostname = new Label { Text = "HOSTNAME:", Location = new Point(10, yPos), Size = new Size(80, 25) };
            txtHostname = new TextBox { Location = new Point(100, yPos), Size = new Size(200, 25), ReadOnly = true };
            panelInfoCliente.Controls.AddRange(new Control[] { lblHostname, txtHostname });

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

            // Botón Reservar IP
            yPos += 130;
            btnReservarIP = new Button
            {
                Text = "Reservar IP Actual",
                Location = new Point(10, yPos),
                Size = new Size(150, 30)
            };
            btnReservarIP.Click += BtnReservarIP_Click;
            panelInfoCliente.Controls.Add(btnReservarIP);

            // Control para tiempo de inactividad
            Label lblTiempoInactividad = new Label
            {
                Text = "Tiempo inactividad (seg):",
                Location = new Point(170, yPos),
                Size = new Size(150, 25)
            };

            nudTiempoInactividad = new NumericUpDown
            {
                Location = new Point(320, yPos),
                Size = new Size(60, 25),
                Minimum = 5,
                Maximum = 60,
                Value = 10
            };
            nudTiempoInactividad.ValueChanged += (s, e) =>
                dhcpManager.TiempoInactividadLimite = (int)nudTiempoInactividad.Value;

            panelInfoCliente.Controls.AddRange(new Control[] { lblTiempoInactividad, nudTiempoInactividad });

            // Botón Prevenir IP
            yPos += spacing;
            btnPrevenirIP = new Button
            {
                Text = "Prevenir IP Actual",
                Location = new Point(10, yPos),
                Size = new Size(150, 30)
            };
            btnPrevenirIP.Click += BtnPrevenirIP_Click;
            panelInfoCliente.Controls.Add(btnPrevenirIP);

            // Mostrar máscara de red calculada (AHORA SÍ dhcpManager ya está inicializado)
            yPos += spacing;
            lblMascara = new Label
            {
                Text = $"Máscara de red: {dhcpManager.MascaraRed}",
                Location = new Point(10, yPos),
                Size = new Size(250, 25),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            panelInfoCliente.Controls.Add(lblMascara);

            this.Controls.Add(listBoxClientes);
            this.Controls.Add(panelInfoCliente);
        }

        private void MostrarPanelDHCP()
        {
            listBoxClientes.Visible = true;
            panelInfoCliente.Visible = true;
            this.Text = "Simulador de Redes - DHCP";
        }

        private void ActualizarListBoxClientes()
        {
            listBoxClientes.DataSource = null;
            listBoxClientes.DataSource = dhcpManager.Clientes;
        }

        private void ListBoxClientes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                MostrarInformacionCliente(cliente);
            }
        }

        private void MostrarInformacionCliente(ClienteDHCP cliente)
        {
            txtMac.Text = cliente.MacAddress;
            txtIP.Text = cliente.IP ?? "No asignada";
            txtHostname.Text = cliente.Hostname;
            chkActivo.Checked = cliente.Activo;

            // Actualizar controles de tráfico
            trackTrafico.Value = cliente.Trafico;
            lblTrafico.Text = $"{cliente.Trafico} Mbps";

            btnIniciarTrafico.Enabled = cliente.Activo && !cliente.MedicionActiva;
            btnDetenerTrafico.Enabled = cliente.MedicionActiva;

            // Actualizar máscara de red (por si cambió)
            lblMascara.Text = $"Máscara de red: {dhcpManager.MascaraRed}";
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

        private void BtnReservarIP_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente && !string.IsNullOrEmpty(cliente.IP))
            {
                dhcpManager.ReservarIP(cliente.MacAddress, cliente.IP);
                MessageBox.Show($"IP {cliente.IP} reservada para {cliente.Hostname}", "Reserva Exitosa");
                ActualizarListBoxClientes();
            }
        }

        private void BtnPrevenirIP_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente && !string.IsNullOrEmpty(cliente.IP))
            {
                dhcpManager.PrevenirIP(cliente.IP);
                MessageBox.Show($"IP {cliente.IP} prevenida/pre-reservada", "IP Prevenida");
            }
        }

        private void OnClienteDesactivado(ClienteDHCP cliente)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnClienteDesactivado(cliente)));
                return;
            }

            if (listBoxClientes.SelectedItem == cliente)
            {
                chkActivo.Checked = false;
                btnIniciarTrafico.Enabled = false;
                btnDetenerTrafico.Enabled = false;
            }

            // Actualizar el ListBox para reflejar el cambio
            ActualizarListBoxClientes();

            MessageBox.Show($"Cliente {cliente.Hostname} desactivado por inactividad", "Cliente Inactivo");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            dhcpManager?.DetenerTodosLosTimers();
            base.OnFormClosing(e);
        }
    }
}