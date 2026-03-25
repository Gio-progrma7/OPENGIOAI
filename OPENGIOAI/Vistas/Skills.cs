using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class Skills : Form
    {
        private Process _procesoActual = null;
        private StringBuilder _outputBuffer = new StringBuilder();

        private List<Skill> ListSkill = new List<Skill>();
        private string RutaSkill = "";

        // Definición de colores del tema profesional
        private readonly Color ColorFondo = Color.FromArgb(15, 23, 42);
        private readonly Color ColorCard = Color.FromArgb(30, 41, 59);
        private readonly Color ColorBorde = Color.FromArgb(51, 65, 85);
        private readonly Color ColorTextoPrincipal = Color.FromArgb(241, 245, 249);
        private readonly Color ColorTextoSecundario = Color.FromArgb(148, 163, 184);
        private readonly Color ColorAcento = Color.FromArgb(37, 99, 235);

        public Skills(string ruta)
        {
            InitializeComponent();
            RutaSkill = ruta;
            this.BackColor = ColorFondo;
        }

        private void Skills_Load(object sender, EventArgs e)
        {
            CargarDatos();
            CargarThema();
        }

        private Panel CrearPanelSkill(Skill _Skill)
        {
            // ===== Panel principal (Card) =====
            Panel panel = new Panel();
            panel.Width = (pnlContenedor.ClientSize.Width / 3) - 20;
            panel.Height = 180; // Un poco más alto para aire visual
            panel.Margin = new Padding(10);
            panel.BackColor = ColorCard;
            panel.Cursor = Cursors.Hand;

            // ===== Icono de Código (Badge) =====
            Panel badge = new Panel();
            badge.Size = new Size(40, 40);
            badge.Location = new Point(16, 16);
            badge.BackColor = Color.FromArgb(51, 65, 85);
            // Simulación de bordes redondeados (Requiere tu utilidad RedondearPanel)
            badge.RedondearPanel(borderRadius: 10, borderColor: Color.Transparent);

            Label lblIcono = new Label();
            lblIcono.Text = "</>"; // Icono tipo código
            lblIcono.Font = new Font("Consolas", 12, FontStyle.Bold);
            lblIcono.ForeColor = Color.FromArgb(96, 165, 250);
            lblIcono.TextAlign = ContentAlignment.MiddleCenter;
            lblIcono.Dock = DockStyle.Fill;
            badge.Controls.Add(lblIcono);

            // ===== Título (Nombre del archivo) =====
            Label lblNombre = new Label();
            lblNombre.Text = Path.GetFileName(_Skill.RutaScript);
            lblNombre.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblNombre.ForeColor = ColorTextoPrincipal;
            lblNombre.Location = new Point(16, 65);
            lblNombre.Width = panel.Width - 32;
            lblNombre.AutoEllipsis = true;

            // ===== Subtítulo (Ruta pequeña) =====
            Label lblRutaSmall = new Label();
            lblRutaSmall.Text = _Skill.RutaScript.ToLower();
            lblRutaSmall.Font = new Font("Consolas", 7, FontStyle.Regular);
            lblRutaSmall.ForeColor = Color.FromArgb(100, 116, 139);
            lblRutaSmall.Location = new Point(16, 88);
            lblRutaSmall.Width = panel.Width - 32;
            lblRutaSmall.AutoEllipsis = true;

            // ===== Descripción =====
            Label lblDescripcion = new Label();
            lblDescripcion.Text = _Skill.Descripcion;
            lblDescripcion.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblDescripcion.ForeColor = ColorTextoSecundario;
            lblDescripcion.Location = new Point(16, 105);
            lblDescripcion.Width = panel.Width - 32;
            lblDescripcion.Height = 35;
            lblDescripcion.AutoEllipsis = true;

            // ===== Contenedor de Botones (Footer de la card) =====
            FlowLayoutPanel flowButtons = new FlowLayoutPanel();
            flowButtons.Dock = DockStyle.Bottom;
            flowButtons.Height = 35;
            flowButtons.Padding = new Padding(10, 0, 0, 0);
            flowButtons.BackColor = Color.FromArgb(30, 41, 59);

            // Estilo común para botones internos
            Action<Button, Color> AplicarEstiloBoton = (btn, col) =>
            {
                btn.Size = new Size(65, 24);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                btn.BackColor = Color.Transparent;
                btn.ForeColor = col;
                btn.Font = new Font("Segoe UI", 7, FontStyle.Bold);
                btn.Cursor = Cursors.Hand;
            };

            Button btnEditar = new Button() { Text = "EDITAR", Tag = _Skill };
            btnEditar.Click += BtnEditarSkill_Click;
            AplicarEstiloBoton(btnEditar, Color.FromArgb(96, 165, 250));

            Button btnAbrir = new Button() { Text = "ABRIR", Tag = _Skill };
            btnAbrir.Click += BtnAbrirSkill_Click;
            AplicarEstiloBoton(btnAbrir, Color.FromArgb(148, 163, 184));

            Button btnEliminar = new Button() { Text = "BORRAR", Tag = _Skill };
            btnEliminar.Click += BtnEliminarSkill_Click;
            AplicarEstiloBoton(btnEliminar, Color.FromArgb(248, 113, 113));

            flowButtons.Controls.AddRange(new Control[] { btnEditar, btnAbrir, btnEliminar });

            // ===== Eventos de Hover =====
            panel.MouseEnter += (s, e) =>
            {
                panel.BackColor = Color.FromArgb(51, 65, 85);

            };
            panel.MouseLeave += (s, e) =>
            {
                panel.BackColor = ColorCard;
            };

            // Agregar controles
            panel.Controls.Add(badge);
            panel.Controls.Add(lblNombre);
            panel.Controls.Add(lblRutaSmall);
            panel.Controls.Add(lblDescripcion);
            panel.Controls.Add(flowButtons);

            // Aplicar redondeado final
            panel.RedondearPanel(borderRadius: 12, borderColor: ColorBorde);

            return panel;
        }

        private void CargarThema()
        {
            panel1.RedondearPanel();
            // Botones Principales con estilo moderno
            btnGuardar.BackColor = Color.FromArgb(30, 41, 59);
            btnGuardar.ForeColor = Color.FromArgb(52, 211, 153);
            btnGuardar.AplicarEstiloOutline(colorBorde: Color.FromArgb(52, 211, 153), borderRadius: 8);


            btnEjecutar.BackColor = ColorAcento;
            btnEjecutar.ForeColor = Color.White;
            btnEjecutar.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnEjecutar.AplicarEstiloOutline(colorBorde: ColorAcento, borderRadius: 8);

            // Inputs con estilo "Slate"
            Color inputBg = Color.FromArgb(15, 23, 42);
            Color inputBorder = Color.FromArgb(51, 65, 85);

            txtNombre.BackColor = inputBg;
            txtNombre.ForeColor = ColorTextoPrincipal;
            txtNombre.RedondearTextBox(borderRadius: 8, borderColor: inputBorder);

            txtRuta.BackColor = inputBg;
            txtRuta.ForeColor = ColorTextoSecundario;
            txtRuta.RedondearTextBox(borderRadius: 8, borderColor: inputBorder);

            txtDescripcion.BackColor = inputBg;
            txtDescripcion.ForeColor = ColorTextoPrincipal;
            txtDescripcion.RedondearTextBox(borderRadius: 8, borderColor: inputBorder);

            // Editor de Código (Naranja oscuro/ámbar para resaltar)
            txtCodigo.BackColor = Color.FromArgb(2, 6, 23);
            txtCodigo.ForeColor = Color.FromArgb(253, 186, 116);
            txtCodigo.Font = new Font("Consolas", 10);
            txtCodigo.RedondearTextBox(borderRadius: 8, borderColor: Color.FromArgb(194, 120, 3));

        }

        // --- Los métodos CargarDatos, MostrarSkill, y Eventos Click se mantienen con tu lógica original ---
        private void CargarDatos()
        {
            try
            {
                ListSkill = JsonManager.Leer<Skill>(RutasProyecto.ObtenerRutaListSkills(RutaSkill));
                pnlContenedor.Controls.Clear();
                foreach (var itemSkill in ListSkill)
                {
                    pnlContenedor.Controls.Add(CrearPanelSkill(itemSkill));
                }
            }
            catch (Exception ex) { /* Manejo de error */ }
        }

        private void CargarCodigo(string ruta)
        {
            if (!File.Exists(ruta))
            {
                txtCodigo.Text = "# Archivo no encontrado: " + ruta;
                return;
            }

            txtCodigo.Text = File.ReadAllText(ruta, Encoding.UTF8);
        }

        private void MostrarSkill(Skill skillSelec)
        {
            txtDescripcion.Text = skillSelec.Descripcion;
            txtRuta.Text = skillSelec.RutaScript;
            txtNombre.Text = Path.GetFileName(skillSelec.RutaScript);
            CargarCodigo(skillSelec.RutaScript);
        }

        private void BtnEditarSkill_Click(object sender, EventArgs e) => MostrarSkill((sender as Button).Tag as Skill);
        private void BtnAbrirSkill_Click(object sender, EventArgs e) { /* Lógica abrir */ }
        private void BtnEliminarSkill_Click(object sender, EventArgs e) { /* Lógica eliminar */ }

        private void btnEjecutar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtRuta.Text))
            {
                MessageBox.Show("Selecciona un skill primero.", "Sin script", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ruta = txtRuta.Text.Trim();

            if (!File.Exists(ruta))
            {
                MessageBox.Show("El archivo no existe:\n" + ruta, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Guardar cambios del editor antes de ejecutar
            File.WriteAllText(ruta, txtCodigo.Text, Encoding.UTF8);

            string extension = Path.GetExtension(ruta).ToLower();
            string ejecutable = "";
            string argumentos = "";

            switch (extension)
            {
                case ".py":
                    ejecutable = "python";
                    argumentos = $"\"{ruta}\"";
                    break;
                case ".ps1":
                    ejecutable = "powershell";
                    argumentos = $"-ExecutionPolicy Bypass -File \"{ruta}\"";
                    break;
                case ".bat":
                case ".cmd":
                    ejecutable = "cmd.exe";
                    argumentos = $"/c \"{ruta}\"";
                    break;
                case ".js":
                    ejecutable = "node";
                    argumentos = $"\"{ruta}\"";
                    break;
                default:
                    MessageBox.Show($"Tipo de script no soportado: {extension}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
            }

            EjecutarScript(ejecutable, argumentos);
        }
        private void EjecutarScript(string ejecutable, string argumentos)
        {
            // Detener proceso previo si existe
            if (_procesoActual != null && !_procesoActual.HasExited)
            {
                _procesoActual.Kill();
                _procesoActual.Dispose();
            }

            _outputBuffer.Clear();
            MostrarOutput("▶ Ejecutando script...\n", Color.FromArgb(52, 211, 153));

            btnEjecutar.Enabled = false;
            btnEjecutar.Text = "Ejecutando...";

            var info = new ProcessStartInfo
            {
                FileName = ejecutable,
                Arguments = argumentos,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            _procesoActual = new Process { StartInfo = info, EnableRaisingEvents = true };

            _procesoActual.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    AppendOutput(e.Data + "\n", ColorTextoSecundario);
            };

            _procesoActual.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    AppendOutput(e.Data + "\n", Color.FromArgb(248, 113, 113));
            };

            _procesoActual.Exited += (s, e) =>
            {
                int codigo = _procesoActual.ExitCode;
                string msg = codigo == 0
                    ? "\n✔ Proceso terminado correctamente.\n"
                    : $"\n✖ Proceso terminado con código de error: {codigo}\n";
                Color color = codigo == 0 ? Color.FromArgb(52, 211, 153) : Color.FromArgb(248, 113, 113);

                AppendOutput(msg, color);

                this.Invoke((Action)(() =>
                {
                    btnEjecutar.Enabled = true;
                    btnEjecutar.Text = "Ejecutar";
                }));
            };

            try
            {
                _procesoActual.Start();
                _procesoActual.BeginOutputReadLine();
                _procesoActual.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MostrarOutput($"✖ No se pudo iniciar el proceso: {ex.Message}\n", Color.FromArgb(248, 113, 113));
                btnEjecutar.Enabled = true;
                btnEjecutar.Text = "Ejecutar";
            }
        }

        // Append thread-safe con color al RichTextBox de output
        private void AppendOutput(string texto, Color color)
        {
            
        }

        private void MostrarOutput(string texto, Color color)
        {
          
            AppendOutput(texto, color);
        }

        // Limpiar proceso al cerrar el form
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_procesoActual != null && !_procesoActual.HasExited)
            {
                _procesoActual.Kill();
                _procesoActual.Dispose();
            }
            base.OnFormClosing(e);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {

        }
    }
}