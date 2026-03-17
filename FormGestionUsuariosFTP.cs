using SimuladorRedes.Clases;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimuladorRedes
{
    /// <summary>Ventana para administrar usuarios de un servidor FTP.</summary>
    public partial class FormGestionUsuariosFTP : Form
    {
        private readonly FTPManager ftpManager;
        private readonly string hostname;
        private ListView lvUsuarios;
        private TextBox txtUser;
        private TextBox txtPass;
        private CheckBox chkVer, chkEditar, chkEliminar;

        public FormGestionUsuariosFTP(FTPManager manager, string hostname)
        {
            this.ftpManager = manager;
            this.hostname = hostname;

            this.Text = $"Gestionar Usuarios — {hostname}";
            this.Size = new Size(560, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // ── Lista de usuarios ─────────────────────────────────
            GroupBox grpLista = new GroupBox
            {
                Text = $"Usuarios registrados en  {hostname}",
                Location = new Point(12, 10),
                Size = new Size(520, 200),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lvUsuarios = new ListView
            {
                Location = new Point(8, 22),
                Size = new Size(504, 140),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvUsuarios.Columns.Add("Usuario", 120);
            lvUsuarios.Columns.Add("Permisos", 150);
            lvUsuarios.Columns.Add("Ver", 50);
            lvUsuarios.Columns.Add("Editar", 50);
            lvUsuarios.Columns.Add("Eliminar", 60);

            Button btnEliminar = new Button
            {
                Text = "🗑  Eliminar seleccionado",
                Location = new Point(8, 166),
                Size = new Size(190, 28),
                BackColor = Color.FromArgb(220, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnEliminar.FlatAppearance.BorderSize = 0;
            btnEliminar.Click += (s, e) =>
            {
                if (lvUsuarios.SelectedItems.Count == 0) return;
                string user = lvUsuarios.SelectedItems[0].Text;
                if (user == "admin")
                { MessageBox.Show("No puedes eliminar al administrador.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                ftpManager.EliminarUsuario(hostname, user);
                CargarLista();
            };

            grpLista.Controls.AddRange(new Control[] { lvUsuarios, btnEliminar });

            // ── Agregar usuario ───────────────────────────────────
            GroupBox grpAgregar = new GroupBox
            {
                Text = "Agregar nuevo usuario",
                Location = new Point(12, 218),
                Size = new Size(520, 150),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Usuario / contraseña
            new Label { Text = "Usuario:", Location = new Point(10, 26), Size = new Size(70, 22), Parent = grpAgregar };
            txtUser = new TextBox { Location = new Point(85, 23), Size = new Size(130, 24) };

            new Label { Text = "Contraseña:", Location = new Point(230, 26), Size = new Size(75, 22), Parent = grpAgregar };
            txtPass = new TextBox { Location = new Point(310, 23), Size = new Size(130, 24), PasswordChar = '●' };

            // Checkboxes de permisos
            new Label { Text = "Permisos:", Location = new Point(10, 60), Size = new Size(70, 22), Parent = grpAgregar };
            chkVer = new CheckBox { Text = "Ver", Location = new Point(85, 58), Size = new Size(70, 22), Checked = true };
            chkEditar = new CheckBox { Text = "Editar", Location = new Point(160, 58), Size = new Size(70, 22), Checked = true };
            chkEliminar = new CheckBox { Text = "Eliminar", Location = new Point(235, 58), Size = new Size(80, 22) };

            CheckBox chkTodos = new CheckBox
            {
                Text = "Todos",
                Location = new Point(325, 58),
                Size = new Size(70, 22)
            };
            chkTodos.CheckedChanged += (s, e) =>
            {
                if (chkTodos.Checked)
                { chkVer.Checked = chkEditar.Checked = chkEliminar.Checked = true; }
            };

            Button btnAgregar = new Button
            {
                Text = "➕  Agregar",
                Location = new Point(10, 100),
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(0, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAgregar.FlatAppearance.BorderSize = 0;
            btnAgregar.Click += BtnAgregar_Click;

            grpAgregar.Controls.AddRange(new Control[]
            {
                txtUser, txtPass,
                chkVer, chkEditar, chkEliminar, chkTodos,
                btnAgregar
            });

            // ── Cerrar ────────────────────────────────────────────
            Button btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(430, 375),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK
            };

            this.Controls.AddRange(new Control[] { grpLista, grpAgregar, btnCerrar });

            CargarLista();
        }

        private void CargarLista()
        {
            lvUsuarios.Items.Clear();
            foreach (var u in ftpManager.ObtenerUsuarios(hostname))
            {
                var item = new ListViewItem(u.Username);
                item.SubItems.Add(u.Permisos.ToString());
                item.SubItems.Add(u.PuedeVer() ? "✔" : "");
                item.SubItems.Add(u.PuedeEditar() ? "✔" : "");
                item.SubItems.Add(u.PuedeEliminar() ? "✔" : "");

                if (u.Permisos == FTPPermiso.Todos)
                    item.BackColor = Color.LightGreen;
                else if (u.PuedeVer() && !u.PuedeEditar())
                    item.BackColor = Color.LightYellow;

                lvUsuarios.Items.Add(item);
            }
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser.Text) ||
                string.IsNullOrWhiteSpace(txtPass.Text))
            { MessageBox.Show("Usuario y contraseña son obligatorios.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            FTPPermiso permisos = FTPPermiso.Ninguno;
            if (chkVer.Checked) permisos |= FTPPermiso.Ver;
            if (chkEditar.Checked) permisos |= FTPPermiso.Editar;
            if (chkEliminar.Checked) permisos |= FTPPermiso.Eliminar;

            ftpManager.AgregarUsuario(hostname,
                new FTPUsuario(txtUser.Text.Trim(), txtPass.Text, permisos));

            txtUser.Clear();
            txtPass.Clear();
            CargarLista();
        }
    }
}