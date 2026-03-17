using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimuladorRedes
{
    /// <summary>Diálogo de autenticación FTP (coincide con el boceto del pizarrón).</summary>
    public partial class FormLoginFTP : Form
    {
        private readonly TextBox txtUsuario;
        private readonly TextBox txtContrasena;
        private readonly Label lblError;

        public string Usuario => txtUsuario.Text.Trim();
        public string Contrasena => txtContrasena.Text;

        public FormLoginFTP(string hostnameServidor)
        {
            this.Text = $"Conectar a PC-Remota";
            this.Size = new Size(330, 240);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ── Encabezado ────────────────────────────────────────
            Panel header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(330, 42),
                BackColor = Color.FromArgb(20, 60, 120)
            };
            header.Controls.Add(new Label
            {
                Text = $"🔒  Conectar a  {hostnameServidor}",
                Location = new Point(10, 10),
                Size = new Size(305, 22),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            });

            // ── Campos ────────────────────────────────────────────
            new Label
            {
                Text = "Usuario:",
                Location = new Point(18, 58),
                Size = new Size(80, 22),
                Parent = this
            };
            txtUsuario = new TextBox
            {
                Location = new Point(105, 55),
                Size = new Size(195, 24),
                Text = "admin"
            };

            new Label
            {
                Text = "Contraseña:",
                Location = new Point(18, 93),
                Size = new Size(80, 22),
                Parent = this
            };
            txtContrasena = new TextBox
            {
                Location = new Point(105, 90),
                Size = new Size(195, 24),
                PasswordChar = '●'
            };

            lblError = new Label
            {
                Location = new Point(18, 122),
                Size = new Size(290, 20),
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 8)
            };

            // ── Botones ───────────────────────────────────────────
            Button btnOk = new Button
            {
                Text = "Conectar",
                Location = new Point(85, 153),
                Size = new Size(95, 32),
                BackColor = Color.FromArgb(0, 140, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;

            Button btnCx = new Button
            {
                Text = "Cancelar",
                Location = new Point(195, 153),
                Size = new Size(95, 32),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            this.AcceptButton = btnOk;
            this.CancelButton = btnCx;

            this.Controls.AddRange(new Control[]
            {
                header,
                txtUsuario, txtContrasena, lblError,
                btnOk, btnCx
            });
        }

        public void MostrarError(string mensaje) => lblError.Text = $"⚠  {mensaje}";
    }
}