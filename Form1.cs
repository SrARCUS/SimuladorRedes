using System;
using System.Drawing;
using System.Linq;
using System.Net;
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
            dhcpManager.ClienteDesactivado += OnClienteDesactivado;

            // DESPUÉS inicializar la interfaz
            InicializarMenu();
            InicializarControles();

            // Agregar clientes de ejemplo
            dhcpManager.AgregarClienteEjemplo(1);
            dhcpManager.AgregarClienteEjemplo(2);

            ActualizarListBoxClientes();
            ActualizarTotalClientes();
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
            this.Size = new Size(950, 650);
            this.Text = "Simulador de Redes - DHCP";

            // Panel superior para controles adicionales
            Panel panelSuperior = new Panel
            {
                Location = new Point(12, 40),
                Size = new Size(900, 80),
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
                Text = "Total clientes: 2",
                Location = new Point(200, 15),
                Size = new Size(150, 25),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // Control para cambiar la red base
            Label lblRedBase = new Label
            {
                Text = "Red Base:",
                Location = new Point(350, 15),
                Size = new Size(70, 25)
            };

            cmbRedBase = new ComboBox
            {
                Location = new Point(420, 12),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRedBase.Items.AddRange(new string[] { "192.168.1", "192.168.0", "10.0.0", "172.16.0" });
            cmbRedBase.SelectedItem = "192.168.1";
            cmbRedBase.SelectedIndexChanged += CmbRedBase_SelectedIndexChanged;

            // Label para información de red
            lblInfoRed = new Label
            {
                Text = $"Máscara actual: {dhcpManager.MascaraRed}",
                Location = new Point(550, 15),
                Size = new Size(300, 25),
                Font = new Font("Arial", 9, FontStyle.Regular)
            };

            panelSuperior.Controls.AddRange(new Control[]
            {
                btnAgregarCliente,
                lblTotalClientes,
                lblRedBase,
                cmbRedBase,
                lblInfoRed
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
                // Salir del modo edición si estábamos en él
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

            // Actualizar la máscara de red según la IP del cliente seleccionado
            if (!string.IsNullOrEmpty(cliente.IP) && cliente.IP != "No asignada")
            {
                lblMascara.Text = $"Máscara de red: {CalcularMascaraPorIP(cliente.IP)}";
            }
            else
            {
                lblMascara.Text = $"Máscara de red: {CalcularMascaraPorIP(dhcpManager.RedBase + ".1")}";
            }

            // Actualizar estado de botones según si el cliente está activo
            btnActivarCliente.Enabled = !cliente.Activo;
            btnDesactivarCliente.Enabled = cliente.Activo;
            btnEditarIP.Enabled = cliente.Activo && !string.IsNullOrEmpty(cliente.IP);

            // Actualizar controles de tráfico
            trackTrafico.Value = cliente.Trafico;
            lblTrafico.Text = $"{cliente.Trafico} Mbps";

            btnIniciarTrafico.Enabled = cliente.Activo && !cliente.MedicionActiva;
            btnDetenerTrafico.Enabled = cliente.MedicionActiva;
        }

        // Método para calcular máscara basada en IP
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

        // Cambiar red base
        private void CmbRedBase_SelectedIndexChanged(object sender, EventArgs e)
        {
            string nuevaRed = cmbRedBase.SelectedItem.ToString();
            dhcpManager.CambiarRedBase(nuevaRed);
            lblInfoRed.Text = $"Red base: {nuevaRed}";

            // Reasignar IPs a todos los clientes
            foreach (var cliente in dhcpManager.Clientes)
            {
                string ipAnterior = cliente.IP;
                cliente.IP = dhcpManager.AsignarIP(cliente);
            }

            ActualizarListBoxClientes();

            // Actualizar la máscara si hay un cliente seleccionado
            if (listBoxClientes.SelectedItem is ClienteDHCP clienteSeleccionado)
            {
                MostrarInformacionCliente(clienteSeleccionado);
            }

            MessageBox.Show($"Red base cambiada a {nuevaRed}.0/24\nLas IPs han sido reasignadas.",
                "Red Cambiada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Iniciar edición de IP
        private void BtnEditarIP_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente && !string.IsNullOrEmpty(cliente.IP))
            {
                modoEdicionIP = true;
                ipOriginalEnEdicion = cliente.IP;

                // Ocultar textbox y mostrar control de edición
                txtIP.Visible = false;
                btnEditarIP.Visible = false;

                // Configurar NumericUpDown con el valor actual
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

        // Guardar edición de IP
        private void BtnGuardarIP_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                string nuevaIP = $"{dhcpManager.RedBase}.{nudOcteto4.Value}";

                // Validar que la IP no esté en uso por otro cliente
                var clienteExistente = dhcpManager.Clientes
                    .FirstOrDefault(c => c.IP == nuevaIP && c != cliente);

                if (clienteExistente != null)
                {
                    MessageBox.Show($"La IP {nuevaIP} ya está siendo usada por {clienteExistente.Hostname}.\nPor favor elige otra IP.",
                        "IP Duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar que la IP no esté prevenida (esto sigue estando en el código interno)
                if (dhcpManager.IPsReservadas.Contains(nuevaIP) && !cliente.TieneReserva)
                {
                    var result = MessageBox.Show($"La IP {nuevaIP} está prevenida/reservada para otro cliente.\n¿Deseas usarla de todas formas?",
                        "IP Reservada", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                        return;
                }

                // Actualizar IP
                string ipAnterior = cliente.IP;
                dhcpManager.LiberarIP(ipAnterior);
                cliente.IP = nuevaIP;
                dhcpManager.IPsOcupadas.Add(nuevaIP);

                // Salir del modo edición
                FinalizarEdicionIP();

                // Actualizar vista
                MostrarInformacionCliente(cliente);
                ActualizarListBoxClientes();

                MessageBox.Show($"IP cambiada de {ipAnterior} a {nuevaIP}", "IP Actualizada",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Cancelar edición de IP
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
            // Encontrar el próximo número disponible para el cliente
            int nextNumber = 1;
            var existingNumbers = dhcpManager.Clientes
                .Select(c => int.Parse(c.Hostname.Replace("Cliente-", "")))
                .OrderBy(n => n)
                .ToList();

            // Buscar el primer número no utilizado
            foreach (int num in existingNumbers)
            {
                if (num == nextNumber)
                    nextNumber++;
                else
                    break;
            }

            // Agregar el nuevo cliente
            dhcpManager.AgregarClienteEjemplo(nextNumber);

            // Actualizar la interfaz
            ActualizarListBoxClientes();
            ActualizarTotalClientes();

            // Seleccionar el nuevo cliente automáticamente
            var nuevoCliente = dhcpManager.Clientes.FirstOrDefault(c => c.Hostname == $"Cliente-{nextNumber}");
            if (nuevoCliente != null)
            {
                listBoxClientes.SelectedItem = nuevoCliente;
            }

            MessageBox.Show($"Nuevo cliente Cliente-{nextNumber} agregado exitosamente.\nMAC: {nuevoCliente.MacAddress}\nIP: {nuevoCliente.IP}",
                "Cliente Agregado", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                cliente.Activar();
                MostrarInformacionCliente(cliente);
                ActualizarListBoxClientes();
                MessageBox.Show($"Cliente {cliente.Hostname} activado manualmente", "Cliente Activado");
            }
        }

        private void BtnDesactivarCliente_Click(object sender, EventArgs e)
        {
            if (listBoxClientes.SelectedItem is ClienteDHCP cliente)
            {
                cliente.Desactivar();
                MostrarInformacionCliente(cliente);
                ActualizarListBoxClientes();
                MessageBox.Show($"Cliente {cliente.Hostname} desactivado manualmente", "Cliente Desactivado");
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
                MostrarInformacionCliente(cliente);
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