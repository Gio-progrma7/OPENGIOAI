// ============================================================
//  Skills.cs — Administrador de Skills (.md) tipo Claude Code
//
//  El editor muestra/edita el archivo .md completo (frontmatter
//  + cuerpo). Al guardar, SkillRunnerHelper extrae el bloque
//  Python y lo escribe como .py listo para ejecutar.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Skills;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class Skills : Form
    {
        // ── Estado ────────────────────────────────────────────────────────────
        private Process?       _procesoActual;
        private List<Skill>    _skills     = new();
        private Skill?         _skillActual;
        private string         RutaSkill   = "";
        private RichTextBox    _rtbOutput  = null!;
        private Button         _btnNuevo   = null!;
        private Label          _lblEditor  = null!;

        // ── Paleta ────────────────────────────────────────────────────────────
        private readonly Color ColorFondo            = Color.FromArgb(15,  23,  42);
        private readonly Color ColorCard             = Color.FromArgb(30,  41,  59);
        private readonly Color ColorBorde            = Color.FromArgb(51,  65,  85);
        private readonly Color ColorTextoPrincipal   = Color.FromArgb(241, 245, 249);
        private readonly Color ColorTextoSecundario  = Color.FromArgb(148, 163, 184);
        private readonly Color ColorAcento           = Color.FromArgb(37,  99,  235);
        private readonly Color ColorVerde            = Color.FromArgb(52,  211, 153);
        private readonly Color ColorRojo             = Color.FromArgb(248, 113, 113);
        private readonly Color ColorAmbar            = Color.FromArgb(251, 191,  36);

        // ── Constructor ───────────────────────────────────────────────────────
        public Skills(string ruta)
        {
            InitializeComponent();
            RutaSkill = ruta;
        }

        // ── Inicialización ────────────────────────────────────────────────────

        private void Skills_Load(object sender, EventArgs e)
        {
            AgregarControlesExtra();
            AplicarThema();
            CargarDatos();
        }

        /// <summary>Agrega controles que no están en el .Designer: output RTB, botón Nuevo.</summary>
        private void AgregarControlesExtra()
        {
            // ── RichTextBox de output (debajo de btnEjecutar) ─────────────────
            _rtbOutput = new RichTextBox
            {
                Location  = new Point(btnGuardar.Left, btnEjecutar.Bottom + 8),
                Size      = new Size(pnlContenedor.Right - btnGuardar.Left, 80),
                BackColor = Color.FromArgb(2, 6, 23),
                ForeColor = ColorTextoSecundario,
                Font      = new Font("Consolas", 8.5f),
                ReadOnly  = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Name        = "rtbOutput"
            };
            Controls.Add(_rtbOutput);

            // Ajustar pnlContenedor para empezar debajo del output
            pnlContenedor.Location = new Point(
                pnlContenedor.Left,
                _rtbOutput.Bottom + 8);
            pnlContenedor.Height = ClientSize.Height - pnlContenedor.Top - 10;

            // ── Botón NUEVO ───────────────────────────────────────────────────
            _btnNuevo = new Button
            {
                Text      = "+ NUEVO",
                Location  = new Point(btnEjecutar.Right + 12, btnEjecutar.Top),
                Size      = new Size(90, btnEjecutar.Height),
                FlatStyle = FlatStyle.Flat,
                ForeColor = ColorAmbar,
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Name      = "btnNuevo"
            };
            _btnNuevo.FlatAppearance.BorderColor = ColorAmbar;
            _btnNuevo.FlatAppearance.BorderSize  = 1;
            _btnNuevo.Click += BtnNuevo_Click;
            Controls.Add(_btnNuevo);

            // ── Label editor ─────────────────────────────────────────────────
            _lblEditor = new Label
            {
                Text      = "Editor de Skill (.md)",
                Location  = new Point(panel1.Left, panel1.Top - 18),
                AutoSize  = true,
                ForeColor = ColorTextoSecundario,
                Font      = new Font("Segoe UI", 8f)
            };
            Controls.Add(_lblEditor);

            // Actualizar label4 para indicar que es el editor md
            label4.Text = "Markdown completo (frontmatter + código)";

            // Quitar labels sobrantes del diseñador (los reemplazamos con el panel)
            label1.Visible = false;
            label2.Visible = false;
        }

        private void AplicarThema()
        {
            BackColor = ColorFondo;

            // Panel editor
            panel1.BackColor = ColorCard;
            panel1.RedondearPanel(borderRadius: 12, borderColor: ColorBorde);

            // Campos de metadata (arriba del panel)
            txtNombre.BackColor     = Color.FromArgb(15, 23, 42);
            txtNombre.ForeColor     = ColorTextoPrincipal;
            txtNombre.Font          = new Font("Segoe UI", 10, FontStyle.Bold);
            txtNombre.ReadOnly      = true;
            txtNombre.BorderStyle   = BorderStyle.None;
            txtNombre.PlaceholderText = "Nombre del skill";

            txtRuta.BackColor       = Color.FromArgb(15, 23, 42);
            txtRuta.ForeColor       = ColorTextoSecundario;
            txtRuta.Font            = new Font("Consolas", 8f);
            txtRuta.ReadOnly        = true;
            txtRuta.BorderStyle     = BorderStyle.None;

            txtDescripcion.BackColor  = Color.FromArgb(15, 23, 42);
            txtDescripcion.ForeColor  = ColorTextoSecundario;
            txtDescripcion.Font       = new Font("Segoe UI", 8.5f);
            txtDescripcion.ReadOnly   = true;
            txtDescripcion.BorderStyle = BorderStyle.None;

            // Editor de markdown (código)
            txtCodigo.BackColor  = Color.FromArgb(2, 6, 23);
            txtCodigo.ForeColor  = Color.FromArgb(167, 243, 208);
            txtCodigo.Font       = new Font("Consolas", 9.5f);
            txtCodigo.WordWrap   = false;

            // Botones
            btnGuardar.BackColor = Color.FromArgb(30, 41, 59);
            btnGuardar.ForeColor = ColorVerde;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.FlatAppearance.BorderColor = ColorVerde;
            btnGuardar.FlatAppearance.BorderSize  = 1;
            btnGuardar.Text   = "Guardar";
            btnGuardar.Font   = new Font("Segoe UI", 8, FontStyle.Bold);
            btnGuardar.Cursor = Cursors.Hand;

            btnEjecutar.BackColor = Color.FromArgb(30, 41, 59);
            btnEjecutar.ForeColor = ColorAmbar;
            btnEjecutar.FlatStyle = FlatStyle.Flat;
            btnEjecutar.FlatAppearance.BorderColor = ColorAmbar;
            btnEjecutar.FlatAppearance.BorderSize  = 1;
            btnEjecutar.Text   = "Probar";
            btnEjecutar.Font   = new Font("Segoe UI", 8, FontStyle.Bold);
            btnEjecutar.Cursor = Cursors.Hand;

            // Label de código
            label4.ForeColor = ColorTextoSecundario;
            label4.Font      = new Font("Segoe UI", 7.5f);

            // Label editor
            _lblEditor.ForeColor = ColorTextoSecundario;

            // Cards container
            pnlContenedor.BackColor = ColorFondo;
        }

        // ── Cargar datos ──────────────────────────────────────────────────────

        private void CargarDatos()
        {
            try
            {
                _skills = SkillLoader.CargarTodas(RutaSkill);
                RenderizarCards();
            }
            catch (Exception ex)
            {
                Log($"Error cargando skills: {ex.Message}", ColorRojo);
            }
        }

        private void RenderizarCards()
        {
            pnlContenedor.Controls.Clear();

            if (_skills.Count == 0)
            {
                var lbl = new Label
                {
                    Text      = "No hay skills. Haz clic en + NUEVO para crear uno.",
                    ForeColor = ColorTextoSecundario,
                    Font      = new Font("Segoe UI", 10f),
                    AutoSize  = true,
                    Margin    = new Padding(20)
                };
                pnlContenedor.Controls.Add(lbl);
                return;
            }

            foreach (var skill in _skills)
                pnlContenedor.Controls.Add(CrearCard(skill));
        }

        // ── Card de skill ─────────────────────────────────────────────────────

        private Panel CrearCard(Skill skill)
        {
            int cardW = Math.Max(220, (pnlContenedor.ClientSize.Width / 3) - 20);

            var panel = new Panel
            {
                Width     = cardW,
                Height    = 170,
                Margin    = new Padding(8),
                BackColor = ColorCard,
                Cursor    = Cursors.Hand,
                Tag       = skill
            };

            // Badge categoría
            var badge = new Label
            {
                Text      = $"[{skill.Categoria.ToUpper()}]",
                Font      = new Font("Consolas", 7, FontStyle.Bold),
                ForeColor = CategoriaColor(skill.Categoria),
                BackColor = Color.FromArgb(15, 23, 42),
                Location  = new Point(12, 12),
                AutoSize  = true,
                Padding   = new Padding(4, 2, 4, 2)
            };

            // Estado activa
            var lblEstado = new Label
            {
                Text      = skill.Activa ? "ACTIVA" : "INACTIVA",
                Font      = new Font("Segoe UI", 6.5f, FontStyle.Bold),
                ForeColor = skill.Activa ? ColorVerde : ColorRojo,
                Location  = new Point(12, badge.Bottom + 6),
                AutoSize  = true
            };

            // Nombre
            var lblNombre = new Label
            {
                Text      = skill.NombreEfectivo,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorTextoPrincipal,
                Location  = new Point(12, lblEstado.Bottom + 4),
                Width     = cardW - 24,
                AutoEllipsis = true
            };

            // Descripción
            var lblDesc = new Label
            {
                Text      = skill.Descripcion,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = ColorTextoSecundario,
                Location  = new Point(12, lblNombre.Bottom + 4),
                Width     = cardW - 24,
                Height    = 32,
                AutoEllipsis = true
            };

            // Barra de botones
            var flow = new FlowLayoutPanel
            {
                Dock      = DockStyle.Bottom,
                Height    = 32,
                Padding   = new Padding(8, 2, 0, 2),
                BackColor = Color.FromArgb(22, 33, 52)
            };

            var btnEditar  = CrearBtnCard("EDITAR",  Color.FromArgb(96, 165, 250));
            var btnAbrir   = CrearBtnCard("ABRIR",   ColorTextoSecundario);
            var btnBorrar  = CrearBtnCard("BORRAR",  ColorRojo);

            btnEditar.Tag  = skill;
            btnAbrir.Tag   = skill;
            btnBorrar.Tag  = skill;

            btnEditar.Click += (s, e) => CargarEnEditor((Skill)((Button)s!).Tag!);
            btnAbrir.Click  += (s, e) => AbrirEnExplorador((Skill)((Button)s!).Tag!);
            btnBorrar.Click += (s, e) => EliminarSkill((Skill)((Button)s!).Tag!);

            flow.Controls.AddRange(new Control[] { btnEditar, btnAbrir, btnBorrar });

            // Hover
            EventHandler hover = (s, e) => panel.BackColor = Color.FromArgb(45, 58, 80);
            EventHandler leave = (s, e) => panel.BackColor = ColorCard;
            foreach (Control c in new Control[] { panel, badge, lblEstado, lblNombre, lblDesc })
            {
                c.MouseEnter += hover;
                c.MouseLeave += leave;
            }

            panel.Controls.Add(badge);
            panel.Controls.Add(lblEstado);
            panel.Controls.Add(lblNombre);
            panel.Controls.Add(lblDesc);
            panel.Controls.Add(flow);
            panel.RedondearPanel(borderRadius: 10, borderColor: ColorBorde);

            return panel;
        }

        private static Button CrearBtnCard(string texto, Color color) => new Button
        {
            Text      = texto,
            Size      = new Size(58, 22),
            FlatStyle = FlatStyle.Flat,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font      = new Font("Segoe UI", 6.5f, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(51, 65, 85) }
        };

        private Color CategoriaColor(string cat) => cat.ToLower() switch
        {
            "sistema"   => Color.FromArgb(96,  165, 250),
            "archivos"  => Color.FromArgb(167, 243, 208),
            "ia"        => Color.FromArgb(216, 180, 254),
            "web"       => Color.FromArgb(253, 186, 116),
            "datos"     => Color.FromArgb(251, 191,  36),
            _           => ColorTextoSecundario
        };

        // ── Editor ────────────────────────────────────────────────────────────

        private void CargarEnEditor(Skill skill)
        {
            _skillActual = skill;

            // Si viene de .md, cargar el .md completo
            if (!string.IsNullOrWhiteSpace(skill.RutaMd) && File.Exists(skill.RutaMd))
            {
                txtCodigo.Text = File.ReadAllText(skill.RutaMd, Encoding.UTF8);
            }
            else if (!string.IsNullOrWhiteSpace(skill.RutaScript))
            {
                // Fallback: cargar solo el .py
                string rutaPy = Path.IsPathRooted(skill.RutaScript)
                    ? skill.RutaScript
                    : Path.Combine(RutaSkill, "skills", skill.RutaScript);
                txtCodigo.Text = File.Exists(rutaPy)
                    ? File.ReadAllText(rutaPy, Encoding.UTF8)
                    : $"# Archivo no encontrado: {rutaPy}";
            }

            // Actualizar campos de metadata (readonly, solo informativos)
            txtNombre.Text      = skill.NombreEfectivo;
            txtRuta.Text        = string.IsNullOrWhiteSpace(skill.RutaMd)
                ? skill.RutaScript
                : skill.RutaMd;
            txtDescripcion.Text = $"Categoría: {skill.Categoria}  |  " +
                                  $"Estado: {(skill.Activa ? "Activa" : "Inactiva")}  |  " +
                                  $"ID: {skill.IdEfectivo}";

            txtCodigo.Focus();
            Log($"Skill cargado: {skill.NombreEfectivo}", ColorVerde);
        }

        // ── Guardar ───────────────────────────────────────────────────────────

        private async void btnGuardar_Click(object sender, EventArgs e)
        {
            if (_skillActual == null)
            {
                Log("Selecciona un skill de la lista antes de guardar.", ColorAmbar);
                return;
            }

            string contenido = txtCodigo.Text;
            if (string.IsNullOrWhiteSpace(contenido))
            {
                Log("El editor está vacío.", ColorRojo);
                return;
            }

            try
            {
                // Determinar ruta .md
                string rutaMd = string.IsNullOrWhiteSpace(_skillActual.RutaMd)
                    ? Path.Combine(RutaSkill, "skills", $"{_skillActual.IdEfectivo}.md")
                    : _skillActual.RutaMd;

                // Asegurar directorio
                Directory.CreateDirectory(Path.GetDirectoryName(rutaMd)!);

                // Guardar .md
                await File.WriteAllTextAsync(rutaMd, contenido, Encoding.UTF8);
                _skillActual.RutaMd     = rutaMd;
                _skillActual.ContenidoMd = SkillMdParser.Parsear(rutaMd)?.ContenidoMd ?? "";

                // Extraer y escribir el .py
                var skillActualizado = SkillMdParser.Parsear(rutaMd);
                if (skillActualizado != null)
                {
                    var skills = SkillLoader.CargarTodas(RutaSkill);
                    await SkillRunnerHelper.ExtraerScriptsMdAsync(
                        RutaSkill,
                        new[] { skillActualizado },
                        CancellationToken.None);
                    await SkillRunnerHelper.GenerarAsync(RutaSkill, skills);
                }

                Log($"Guardado: {Path.GetFileName(rutaMd)}", ColorVerde);
                CargarDatos();
            }
            catch (Exception ex)
            {
                Log($"Error guardando: {ex.Message}", ColorRojo);
            }
        }

        // ── Nuevo skill ───────────────────────────────────────────────────────

        private void BtnNuevo_Click(object sender, EventArgs e)
        {
            string nombre = MostrarInputDialog(
                "Nombre del nuevo skill (ej: enviar_email):",
                "Nuevo Skill", "mi_skill");

            if (string.IsNullOrWhiteSpace(nombre)) return;

            string id      = nombre.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            string carpeta = Path.Combine(RutaSkill, "skills");
            Directory.CreateDirectory(carpeta);

            string rutaMd  = Path.Combine(carpeta, $"{id}.md");

            if (File.Exists(rutaMd))
            {
                Log($"Ya existe un skill con ese ID: {id}", ColorAmbar);
                // Cargar el existente
                var existente = SkillMdParser.Parsear(rutaMd);
                if (existente != null) CargarEnEditor(existente);
                return;
            }

            string template = $@"---
id: {id}
nombre: {System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace("_", " "))}
categoria: general
descripcion: Descripción breve de lo que hace este skill
activa: true
ejemplo: skill_run(""{id}"")
---

## Descripción
Describe aquí qué hace el skill, cuándo usarlo y qué devuelve.

## Código
```python
import os, sys, json, time
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params = json.loads(os.environ.get(""SKILL_PARAMS"", ""{{}}""))

    try:
        # ── LÓGICA PRINCIPAL ──────────────────────────────────
        resultado = {{
            ""status"": ""ok"",
            ""accion"": ""{id}"",
            ""timestamp"": datetime.now(timezone.utc).isoformat(),
            ""duracion_segundos"": round(time.time() - inicio, 3),
            ""resultado"": ""Implementa tu lógica aquí"",
            ""detalle"": """"
        }}
    except Exception as e:
        resultado = {{
            ""status"": ""error"",
            ""accion"": ""{id}"",
            ""timestamp"": datetime.now(timezone.utc).isoformat(),
            ""duracion_segundos"": round(time.time() - inicio, 3),
            ""nivel"": ""ALTO"",
            ""detalle"": str(e),
            ""sugerencia"": ""Revisa los parámetros.""
        }}

    print(json.dumps(resultado, ensure_ascii=False))

if __name__ == ""__main__"":
    main()
```

## Parámetros
- nombre: ejemplo | tipo: string | requerido: false | descripcion: Parámetro de ejemplo
";

            File.WriteAllText(rutaMd, template, Encoding.UTF8);

            CargarDatos();

            // Seleccionar el recién creado
            var nuevoSkill = SkillMdParser.Parsear(rutaMd);
            if (nuevoSkill != null)
                CargarEnEditor(nuevoSkill);

            Log($"Skill creado: {id}.md", ColorVerde);
        }

        // ── Probar (ejecutar) ─────────────────────────────────────────────────

        private async void btnEjecutar_Click(object sender, EventArgs e)
        {
            if (_skillActual == null)
            {
                Log("Selecciona un skill primero.", ColorAmbar);
                return;
            }

            // Guardar antes de probar
            btnGuardar_Click(sender, e);
            await Task.Delay(300); // dar tiempo al guardado async

            // Resolver ruta del .py
            string idSkill = _skillActual.IdEfectivo;
            string rutaPy  = Path.Combine(RutaSkill, "skills", $"{idSkill}.py");

            if (!File.Exists(rutaPy))
            {
                Log($"Script no encontrado: {rutaPy}", ColorRojo);
                return;
            }

            EjecutarScript("python", $"\"{rutaPy}\"");
        }

        private void EjecutarScript(string ejecutable, string argumentos)
        {
            if (_procesoActual != null && !_procesoActual.HasExited)
            {
                _procesoActual.Kill();
                _procesoActual.Dispose();
            }

            _rtbOutput.Clear();
            Log("Ejecutando skill...", ColorAmbar);

            btnEjecutar.Enabled = false;
            btnEjecutar.Text    = "Ejecutando...";

            var info = new ProcessStartInfo
            {
                FileName               = ejecutable,
                Arguments              = argumentos,
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };

            _procesoActual = new Process
            {
                StartInfo           = info,
                EnableRaisingEvents = true
            };

            _procesoActual.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    AppendOutput(e.Data + "\n", ColorTextoSecundario);
            };

            _procesoActual.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    AppendOutput(e.Data + "\n", ColorRojo);
            };

            _procesoActual.Exited += (s, e) =>
            {
                int codigo = _procesoActual!.ExitCode;
                string msg = codigo == 0
                    ? "Proceso terminado correctamente.\n"
                    : $"Proceso terminado con error (codigo {codigo})\n";
                AppendOutput(msg, codigo == 0 ? ColorVerde : ColorRojo);

                if (IsHandleCreated)
                    Invoke(() =>
                    {
                        btnEjecutar.Enabled = true;
                        btnEjecutar.Text    = "Probar";
                    });
            };

            try
            {
                _procesoActual.Start();
                _procesoActual.BeginOutputReadLine();
                _procesoActual.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log($"No se pudo ejecutar: {ex.Message}", ColorRojo);
                btnEjecutar.Enabled = true;
                btnEjecutar.Text    = "Probar";
            }
        }

        // ── Acciones de card ──────────────────────────────────────────────────

        private void AbrirEnExplorador(Skill skill)
        {
            string ruta = string.IsNullOrWhiteSpace(skill.RutaMd)
                ? Path.Combine(RutaSkill, "skills")
                : Path.GetDirectoryName(skill.RutaMd) ?? RutaSkill;

            try { Process.Start("explorer.exe", $"\"{ruta}\""); }
            catch (Exception ex) { Log($"No se pudo abrir el explorador: {ex.Message}", ColorRojo); }
        }

        private void EliminarSkill(Skill skill)
        {
            string nombre = skill.NombreEfectivo;
            var r = MessageBox.Show(
                $"¿Eliminar el skill '{nombre}'?\n\nSe borrarán el .md y el .py generado.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (r != DialogResult.Yes) return;

            try
            {
                if (!string.IsNullOrWhiteSpace(skill.RutaMd))
                    SkillLoader.EliminarMd(skill.RutaMd);

                // Borrar el .py generado
                string rutaPy = Path.Combine(RutaSkill, "skills", $"{skill.IdEfectivo}.py");
                if (File.Exists(rutaPy)) File.Delete(rutaPy);

                if (_skillActual?.IdEfectivo == skill.IdEfectivo)
                {
                    _skillActual    = null;
                    txtCodigo.Text  = "";
                    txtNombre.Text  = "";
                    txtRuta.Text    = "";
                    txtDescripcion.Text = "";
                }

                Log($"Skill eliminado: {nombre}", ColorAmbar);
                CargarDatos();
            }
            catch (Exception ex)
            {
                Log($"Error eliminando skill: {ex.Message}", ColorRojo);
            }
        }

        // ── Output helpers ────────────────────────────────────────────────────

        private void AppendOutput(string texto, Color color)
        {
            if (!IsHandleCreated) return;
            if (InvokeRequired)
            {
                Invoke(() => AppendOutput(texto, color));
                return;
            }
            _rtbOutput.SelectionStart  = _rtbOutput.TextLength;
            _rtbOutput.SelectionLength = 0;
            _rtbOutput.SelectionColor  = color;
            _rtbOutput.AppendText(texto);
            _rtbOutput.ScrollToCaret();
        }

        private void Log(string msg, Color? color = null)
            => AppendOutput($"[{DateTime.Now:HH:mm:ss}] {msg}\n", color ?? ColorTextoSecundario);

        // ── Input dialog helper ───────────────────────────────────────────────

        private string MostrarInputDialog(string mensaje, string titulo, string valorInicial = "")
        {
            using var dlg = new Form
            {
                Text            = titulo,
                Size            = new Size(400, 140),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition   = FormStartPosition.CenterParent,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = ColorCard
            };

            var lbl = new Label
            {
                Text      = mensaje,
                Location  = new Point(12, 12),
                AutoSize  = true,
                ForeColor = ColorTextoPrincipal,
                Font      = new Font("Segoe UI", 9f)
            };

            var txt = new TextBox
            {
                Location  = new Point(12, 38),
                Width     = 360,
                Text      = valorInicial,
                BackColor = ColorFondo,
                ForeColor = ColorTextoPrincipal,
                Font      = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnOk = new Button
            {
                Text         = "Crear",
                DialogResult = DialogResult.OK,
                Location     = new Point(220, 68),
                Size         = new Size(75, 28),
                FlatStyle    = FlatStyle.Flat,
                ForeColor    = ColorVerde,
                BackColor    = Color.Transparent,
                Font         = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btnOk.FlatAppearance.BorderColor = ColorVerde;

            var btnCancel = new Button
            {
                Text         = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location     = new Point(300, 68),
                Size         = new Size(75, 28),
                FlatStyle    = FlatStyle.Flat,
                ForeColor    = ColorRojo,
                BackColor    = Color.Transparent,
                Font         = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderColor = ColorRojo;

            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;
            dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

            txt.SelectAll();
            return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : "";
        }

        // ── Form closing ──────────────────────────────────────────────────────

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_procesoActual != null && !_procesoActual.HasExited)
            {
                _procesoActual.Kill();
                _procesoActual.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
