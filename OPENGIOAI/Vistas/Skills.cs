// ============================================================
//  Skills.cs — Administrador de Skills (.md) tipo Claude Code
//
//  El editor muestra/edita el archivo .md completo (frontmatter
//  + cuerpo). Al guardar, SkillRunnerHelper extrae el bloque
//  Python y lo escribe como .py listo para ejecutar.
// ============================================================

using Newtonsoft.Json.Linq;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosAI;
using OPENGIOAI.Skills;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        // ── Referencia al TabControl inferior ────────────────────────────────
        private TabControl     _tabInferior     = null!;

        // ── Tab Parámetros (editor visual) ───────────────────────────────────
        private TabPage          _tabParams      = null!;
        private DataGridView     _gridParams     = null!;
        private Label            _lblParamsHint  = null!;
        private Button           _btnParamAdd    = null!;
        private Button           _btnParamEdit   = null!;
        private Button           _btnParamDelete = null!;
        private Button           _btnParamUp     = null!;
        private Button           _btnParamDown   = null!;
        private List<SkillParametro> _paramsActuales = new();

        // ── Hub ───────────────────────────────────────────────────────────────
        private Panel          _pnlHub         = null!;
        private TextBox        _txtHubUrl       = null!;
        private Button         _btnHubInstalar  = null!;
        private Button         _btnHub          = null!;
        private Label          _lblHubEstado    = null!;
        private FlowLayoutPanel _flpHubCards    = null!;
        private CancellationTokenSource? _ctsHub;

        // ── Agente Creador ────────────────────────────────────────────────────
        private Panel          _pnlCreador      = null!;
        private TextBox        _txtDescripcion  = null!;
        private RichTextBox    _rtbCreadorLog   = null!;
        private Button         _btnCrearIA      = null!;
        private Button         _btnCrearIAStart = null!;
        private Label          _lblCreadorEstado= null!;
        private Label          _lblFaseActual   = null!;
        private ProgressBar    _pbCreador        = null!;
        private Panel          _pnlFases         = null!;
        private CancellationTokenSource? _ctsCreador;

        // ── Config LLM (necesaria para el agente creador) ─────────────────────
        private readonly ConfiguracionClient _config;
        private List<Api> _listaApis = new();
        private string Modelo    => _config?.Mimodelo?.Modelos ?? "";
        private string ApiKey    => _config?.Mimodelo?.ApiKey  ?? "";
        private Servicios Agente => _config?.Mimodelo?.Agente  ?? Servicios.Gemenni;
        private string ClavesApis => Utils.ObtenerNombresApis(_listaApis);

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
        public Skills(ConfiguracionClient config)
        {
            InitializeComponent();
            _config   = config ?? new ConfiguracionClient();
            RutaSkill = config?.MiArchivo?.Ruta ?? RutasProyecto.ObtenerRutaScripts();
        }

        // ── Inicialización ────────────────────────────────────────────────────

        private void Skills_Load(object sender, EventArgs e)
        {
            _listaApis = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());
            AgregarControlesExtra();
            AplicarThema();
            CargarDatos();
        }

        /// <summary>Agrega controles que no están en el .Designer: output RTB, botón Nuevo.</summary>
        private void AgregarControlesExtra()
        {
            // ── RichTextBox de output (vive dentro del tab Log) ───────────────
            _rtbOutput = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = Color.FromArgb(2, 6, 23),
                ForeColor   = ColorTextoSecundario,
                Font        = new Font("Consolas", 9f),
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Name        = "rtbOutput"
            };

            // ── Tab: 📋 Skills ────────────────────────────────────────────────
            var tabSkills = new TabPage
            {
                Text      = "  📋  Skills  ",
                BackColor = Color.FromArgb(15, 23, 42),
                Padding   = new Padding(0)
            };
            pnlContenedor.Dock = DockStyle.Fill;
            tabSkills.Controls.Add(pnlContenedor);

            // ── Tab: 🔧 Parámetros ────────────────────────────────────────────
            ConstruirTabParametros();

            // ── Tab: 📄 Output / Log ──────────────────────────────────────────
            var tabLog = new TabPage
            {
                Text      = "  📄  Output / Log  ",
                BackColor = Color.FromArgb(2, 6, 18),
                Padding   = new Padding(0)
            };
            tabLog.Controls.Add(_rtbOutput);

            // ── TabControl inferior ───────────────────────────────────────────
            var tabInferior = new TabControl
            {
                Anchor    = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location  = new Point(btnGuardar.Left, btnEjecutar.Bottom + 10),
                Size      = new Size(ClientSize.Width - btnGuardar.Left - 10,
                                     ClientSize.Height - btnEjecutar.Bottom - 20),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Name      = "tabInferior"
            };
            tabInferior.TabPages.Add(tabSkills);
            tabInferior.TabPages.Add(_tabParams);
            tabInferior.TabPages.Add(tabLog);
            Controls.Add(tabInferior);
            _tabInferior = tabInferior;

            // Quitar pnlContenedor del form (ahora vive en el tab)
            Controls.Remove(pnlContenedor);

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

            // ── Botón 🌐 Hub ──────────────────────────────────────────────────
            _btnHub = new Button
            {
                Text      = "🌐 Hub",
                Location  = new Point(_btnNuevo.Right + 10, _btnNuevo.Top),
                Size      = new Size(80, _btnNuevo.Height),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(129, 140, 248), // indigo
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Name      = "btnHub"
            };
            _btnHub.FlatAppearance.BorderColor = Color.FromArgb(129, 140, 248);
            _btnHub.FlatAppearance.BorderSize  = 1;
            _btnHub.Click += (_, _) => AlternarPanelHub();
            Controls.Add(_btnHub);

            // ── Botón 🤖 Crear con IA ────────────────────────────────────────
            _btnCrearIA = new Button
            {
                Text      = "🤖 Crear con IA",
                Location  = new Point(_btnHub.Right + 10, _btnHub.Top),
                Size      = new Size(110, _btnHub.Height),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(167, 243, 208),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Name      = "btnCrearIA"
            };
            _btnCrearIA.FlatAppearance.BorderColor = Color.FromArgb(52, 211, 153);
            _btnCrearIA.FlatAppearance.BorderSize  = 1;
            _btnCrearIA.Click += (_, _) => AlternarPanelCreador();
            Controls.Add(_btnCrearIA);

            // ── Panel Hub (overlay sobre pnlContenedor) ───────────────────────
            ConstruirPanelHub();

            // ── Panel Agente Creador ──────────────────────────────────────────
            ConstruirPanelCreador();
        }

        // ── Tab Parámetros (editor visual) ───────────────────────────────────

        /// <summary>
        /// Construye el tab "🔧 Parámetros" con grid + toolbar.
        /// El usuario edita los parámetros del skill activo sin tocar el markdown:
        /// el grid es la fuente de verdad y al cambiar se reescribe la sección
        /// ## Parámetros en txtCodigo automáticamente.
        /// </summary>
        private void ConstruirTabParametros()
        {
            _tabParams = new TabPage
            {
                Text      = "  🔧  Parámetros  ",
                BackColor = ColorFondo,
                Padding   = new Padding(0)
            };

            // ── Hint superior ────────────────────────────────────────────────
            _lblParamsHint = new Label
            {
                Dock      = DockStyle.Top,
                Height    = 36,
                Text      = "Define aquí los parámetros que el skill aceptará. " +
                            "Se sincronizan automáticamente con la sección " +
                            "'## Parámetros' del markdown.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTextoSecundario,
                BackColor = Color.FromArgb(22, 33, 52),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(12, 0, 12, 0)
            };

            // ── Toolbar ──────────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 40,
                BackColor = ColorCard
            };

            _btnParamAdd    = CrearBtnToolbar("➕  Añadir",   ColorVerde);
            _btnParamEdit   = CrearBtnToolbar("✏  Editar",   Color.FromArgb(96, 165, 250));
            _btnParamDelete = CrearBtnToolbar("🗑  Eliminar", ColorRojo);
            _btnParamUp     = CrearBtnToolbar("▲",            ColorTextoSecundario);
            _btnParamDown   = CrearBtnToolbar("▼",            ColorTextoSecundario);

            _btnParamAdd.Click    += (_, _) => OnParamAdd();
            _btnParamEdit.Click   += (_, _) => OnParamEdit();
            _btnParamDelete.Click += (_, _) => OnParamDelete();
            _btnParamUp.Click     += (_, _) => OnParamMove(-1);
            _btnParamDown.Click   += (_, _) => OnParamMove(+1);

            int x = 8;
            foreach (var b in new[] { _btnParamAdd, _btnParamEdit, _btnParamDelete })
            {
                b.Location = new Point(x, 6);
                toolbar.Controls.Add(b);
                x += b.Width + 6;
            }
            x += 12;
            _btnParamUp.Width = 40;
            _btnParamDown.Width = 40;
            _btnParamUp.Location   = new Point(x, 6); x += 44;
            _btnParamDown.Location = new Point(x, 6);
            toolbar.Controls.Add(_btnParamUp);
            toolbar.Controls.Add(_btnParamDown);

            // ── Grid ─────────────────────────────────────────────────────────
            _gridParams = new DataGridView
            {
                Dock                       = DockStyle.Fill,
                BackgroundColor            = ColorFondo,
                BorderStyle                = BorderStyle.None,
                AllowUserToAddRows         = false,
                AllowUserToDeleteRows      = false,
                AllowUserToResizeRows      = false,
                ReadOnly                   = true,
                MultiSelect                = false,
                SelectionMode              = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode        = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible          = false,
                EnableHeadersVisualStyles  = false,
                GridColor                  = ColorBorde,
                Font                       = new Font("Segoe UI", 9f),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = ColorCard,
                    ForeColor = ColorTextoPrincipal,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleLeft,
                    Padding   = new Padding(8, 0, 8, 0),
                    SelectionBackColor = ColorCard,
                    SelectionForeColor = ColorTextoPrincipal,
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = ColorFondo,
                    ForeColor          = ColorTextoPrincipal,
                    SelectionBackColor = Color.FromArgb(45, 58, 80),
                    SelectionForeColor = ColorTextoPrincipal,
                    Padding            = new Padding(8, 0, 8, 0),
                },
                ColumnHeadersHeight        = 30,
                RowTemplate                = { Height = 26 },
            };

            _gridParams.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { HeaderText = "Nombre",      Name = "nombre",     FillWeight = 22 },
                new DataGridViewTextBoxColumn { HeaderText = "Tipo",        Name = "tipo",       FillWeight = 12 },
                new DataGridViewTextBoxColumn { HeaderText = "Req",         Name = "requerido",  FillWeight =  8 },
                new DataGridViewTextBoxColumn { HeaderText = "Default",     Name = "default",    FillWeight = 14 },
                new DataGridViewTextBoxColumn { HeaderText = "Opciones",    Name = "opciones",   FillWeight = 14 },
                new DataGridViewTextBoxColumn { HeaderText = "Descripción", Name = "descripcion",FillWeight = 30 },
            });

            _gridParams.CellDoubleClick += (_, _) => OnParamEdit();

            // ── Empty state ──────────────────────────────────────────────────
            _tabParams.Controls.Add(_gridParams);
            _tabParams.Controls.Add(toolbar);
            _tabParams.Controls.Add(_lblParamsHint);
        }

        private static Button CrearBtnToolbar(string texto, Color color) => new Button
        {
            Text      = texto,
            Size      = new Size(96, 28),
            FlatStyle = FlatStyle.Flat,
            ForeColor = color,
            BackColor = Color.FromArgb(15, 23, 42),
            Cursor    = Cursors.Hand,
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            FlatAppearance = { BorderColor = Color.FromArgb(51, 65, 85), BorderSize = 1 }
        };

        // ── Acciones del tab Parámetros ──────────────────────────────────────

        private void OnParamAdd()
        {
            if (_skillActual == null)
            {
                Log("Selecciona un skill para añadirle parámetros.", ColorAmbar);
                return;
            }
            var nuevo = DialogoEditarParametro.Mostrar(null, this);
            if (nuevo == null) return;

            // Evitar duplicados por nombre
            if (_paramsActuales.Any(p => p.Nombre.Equals(nuevo.Nombre, StringComparison.OrdinalIgnoreCase)))
            {
                Log($"Ya existe un parámetro con nombre '{nuevo.Nombre}'.", ColorRojo);
                return;
            }

            _paramsActuales.Add(nuevo);
            SincronizarParametros();
        }

        private void OnParamEdit()
        {
            if (_skillActual == null) return;
            int idx = ParamSeleccionado();
            if (idx < 0) return;

            var existente = _paramsActuales[idx];
            var editado = DialogoEditarParametro.Mostrar(existente, this);
            if (editado == null) return;

            // Si renombra, validar duplicado
            if (!editado.Nombre.Equals(existente.Nombre, StringComparison.OrdinalIgnoreCase) &&
                _paramsActuales.Any(p => p.Nombre.Equals(editado.Nombre, StringComparison.OrdinalIgnoreCase)))
            {
                Log($"Ya existe un parámetro con nombre '{editado.Nombre}'.", ColorRojo);
                return;
            }

            _paramsActuales[idx] = editado;
            SincronizarParametros(idx);
        }

        private void OnParamDelete()
        {
            if (_skillActual == null) return;
            int idx = ParamSeleccionado();
            if (idx < 0) return;

            var p = _paramsActuales[idx];
            var r = MessageBox.Show(
                $"¿Eliminar el parámetro '{p.Nombre}'?",
                "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;

            _paramsActuales.RemoveAt(idx);
            SincronizarParametros(Math.Min(idx, _paramsActuales.Count - 1));
        }

        private void OnParamMove(int delta)
        {
            if (_skillActual == null) return;
            int idx = ParamSeleccionado();
            if (idx < 0) return;

            int dest = idx + delta;
            if (dest < 0 || dest >= _paramsActuales.Count) return;

            (_paramsActuales[idx], _paramsActuales[dest]) =
                (_paramsActuales[dest], _paramsActuales[idx]);

            SincronizarParametros(dest);
        }

        private int ParamSeleccionado()
        {
            if (_gridParams.SelectedRows.Count == 0)
            {
                if (_gridParams.CurrentRow != null) return _gridParams.CurrentRow.Index;
                return -1;
            }
            return _gridParams.SelectedRows[0].Index;
        }

        /// <summary>
        /// Refresca el grid desde <see cref="_paramsActuales"/> y reescribe la
        /// sección ## Parámetros en txtCodigo. Llamar tras cualquier cambio.
        /// </summary>
        private void SincronizarParametros(int filaSeleccionada = -1)
        {
            // 1. Refrescar grid
            RefrescarGridParametros();

            if (filaSeleccionada >= 0 && filaSeleccionada < _gridParams.Rows.Count)
            {
                _gridParams.ClearSelection();
                _gridParams.Rows[filaSeleccionada].Selected = true;
                _gridParams.CurrentCell = _gridParams.Rows[filaSeleccionada].Cells[0];
            }

            // 2. Reescribir la sección ## Parámetros en el editor de markdown
            string markdown = txtCodigo.Text ?? "";
            string actualizado = SkillMdParser.ActualizarSeccionParametros(
                markdown, _paramsActuales);

            if (actualizado != markdown)
            {
                int caretAnterior = txtCodigo.SelectionStart;
                txtCodigo.Text = actualizado;
                // No reposicionar caret si el usuario no estaba editando
                txtCodigo.SelectionStart = Math.Min(caretAnterior, txtCodigo.TextLength);
            }

            // 3. Actualizar resumen en la cabecera del editor
            if (_skillActual != null)
            {
                _skillActual.Parametros = new List<SkillParametro>(_paramsActuales);
                string firma = ConstruirFirmaParametros(_skillActual);
                txtDescripcion.Text = $"Categoría: {_skillActual.Categoria}  |  " +
                                      $"Estado: {(_skillActual.Activa ? "Activa" : "Inactiva")}  |  " +
                                      $"ID: {_skillActual.IdEfectivo}" +
                                      (string.IsNullOrEmpty(firma) ? "" : $"\r\nParámetros: {firma}");
            }
        }

        private void RefrescarGridParametros()
        {
            _gridParams.Rows.Clear();
            foreach (var p in _paramsActuales)
            {
                int rowIdx = _gridParams.Rows.Add(
                    p.Nombre,
                    p.Tipo,
                    p.Requerido ? "✓" : "",
                    p.ValorPorDefecto,
                    p.Opciones != null && p.Opciones.Count > 0
                        ? string.Join(", ", p.Opciones)
                        : "",
                    p.Descripcion);

                // Resaltar requeridos
                if (p.Requerido)
                {
                    _gridParams.Rows[rowIdx].Cells["nombre"].Style.ForeColor = ColorAmbar;
                    _gridParams.Rows[rowIdx].Cells["nombre"].Style.Font =
                        new Font("Segoe UI", 9f, FontStyle.Bold);
                    _gridParams.Rows[rowIdx].Cells["requerido"].Style.ForeColor = ColorVerde;
                }
            }
        }

        /// <summary>
        /// Construye el panel flotante del Hub y lo añade al formulario.
        /// El panel se posiciona sobre pnlContenedor y empieza oculto.
        /// </summary>
        private void ConstruirPanelHub()
        {
            _pnlHub = new Panel
            {
                Location  = pnlContenedor.Location,
                Size      = pnlContenedor.Size,
                BackColor = Color.FromArgb(10, 14, 30),
                Visible   = false,
                Name      = "pnlHub"
            };
            _pnlHub.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(99, 102, 241), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlHub.Width - 1, _pnlHub.Height - 1);
            };

            // ── Encabezado ────────────────────────────────────────────────────
            var lblTitulo = new Label
            {
                Text      = "🌐  SKILLS HUB",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(165, 180, 252),
                Location  = new Point(16, 14),
                AutoSize  = true
            };

            var btnCerrar = new Button
            {
                Text      = "✕",
                Size      = new Size(28, 24),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Location = new Point(_pnlHub.Width - 38, 10);
            btnCerrar.Click   += (_, _) => AlternarPanelHub();
            _pnlHub.Resize    += (_, _) => btnCerrar.Left = _pnlHub.Width - 38;

            // ── Separador ─────────────────────────────────────────────────────
            var sep1 = new Panel
            {
                Location  = new Point(0, 42),
                Size      = new Size(_pnlHub.Width, 1),
                BackColor = Color.FromArgb(40, 99, 102, 241),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlHub.Resize += (_, _) => sep1.Width = _pnlHub.Width;

            // ── Sección: Instalar desde URL ───────────────────────────────────
            var lblInstalar = new Label
            {
                Text      = "Instalar desde URL",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location  = new Point(16, 54),
                AutoSize  = true
            };

            var lblHint = new Label
            {
                Text      = "Pega la URL de cualquier .md con frontmatter válido (GitHub raw, etc.)",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location  = new Point(16, 72),
                AutoSize  = true
            };

            _txtHubUrl = new TextBox
            {
                Location    = new Point(16, 90),
                Size        = new Size(_pnlHub.Width - 140, 26),
                BackColor   = Color.FromArgb(15, 23, 42),
                ForeColor   = Color.FromArgb(226, 232, 240),
                Font        = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "https://raw.githubusercontent.com/usuario/repo/main/skills/mi_skill.md",
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlHub.Resize += (_, _) => _txtHubUrl.Width = _pnlHub.Width - 140;

            _btnHubInstalar = new Button
            {
                Text      = "📥 Instalar",
                Location  = new Point(_txtHubUrl.Right + 8, 89),
                Size      = new Size(108, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(52, 211, 153),
                BackColor = Color.FromArgb(6, 78, 59),
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnHubInstalar.FlatAppearance.BorderColor = Color.FromArgb(52, 211, 153);
            _btnHubInstalar.FlatAppearance.BorderSize  = 1;
            _btnHubInstalar.Click += BtnHubInstalar_Click;
            _pnlHub.Resize += (_, _) => _btnHubInstalar.Left = _pnlHub.Width - 120;

            _lblHubEstado = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location  = new Point(16, 122),
                Size      = new Size(_pnlHub.Width - 32, 18),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlHub.Resize += (_, _) => _lblHubEstado.Width = _pnlHub.Width - 32;

            // ── Separador 2 ───────────────────────────────────────────────────
            var sep2 = new Panel
            {
                Location  = new Point(0, 146),
                Size      = new Size(_pnlHub.Width, 1),
                BackColor = Color.FromArgb(30, 99, 102, 241),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlHub.Resize += (_, _) => sep2.Width = _pnlHub.Width;

            // ── Sección: Skills del Hub ───────────────────────────────────────
            var lblInstalados = new Label
            {
                Text      = "Skills instalados desde Hub",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location  = new Point(16, 156),
                AutoSize  = true
            };

            _flpHubCards = new FlowLayoutPanel
            {
                Location    = new Point(8, 178),
                Size        = new Size(_pnlHub.Width - 16, _pnlHub.Height - 230),
                AutoScroll  = true,
                BackColor   = Color.Transparent,
                Anchor      = AnchorStyles.Top | AnchorStyles.Bottom |
                              AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight
            };

            // ── Sección: Exportar skill actual ────────────────────────────────
            var pnlExportar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 44,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            var lblExportar = new Label
            {
                Text      = "Exportar skill actual al portapapeles:",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(12, 12),
                AutoSize  = true
            };

            var btnExportar = new Button
            {
                Text      = "📋 Copiar .md",
                Size      = new Size(110, 26),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(129, 140, 248),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnExportar.FlatAppearance.BorderColor = Color.FromArgb(99, 102, 241);
            btnExportar.FlatAppearance.BorderSize  = 1;
            btnExportar.Location = new Point(pnlExportar.Width - 122, 8);
            pnlExportar.Resize  += (_, _) => btnExportar.Left = pnlExportar.Width - 122;
            btnExportar.Click   += BtnExportarSkill_Click;

            pnlExportar.Controls.AddRange(new Control[] { lblExportar, btnExportar });

            // ── Ensamblar panel ────────────────────────────────────────────────
            _pnlHub.Controls.AddRange(new Control[]
            {
                lblTitulo, btnCerrar, sep1,
                lblInstalar, lblHint, _txtHubUrl, _btnHubInstalar, _lblHubEstado,
                sep2, lblInstalados, _flpHubCards,
                pnlExportar
            });

            Controls.Add(_pnlHub);
            _pnlHub.BringToFront();
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

            // TabControl inferior — tema oscuro
            if (_tabInferior != null)
            {
                _tabInferior.BackColor   = ColorFondo;
                _tabInferior.ForeColor   = ColorTextoSecundario;
                _tabInferior.DrawMode    = TabDrawMode.OwnerDrawFixed;
                _tabInferior.ItemSize    = new Size(0, 30);
                _tabInferior.DrawItem   += (s, e) =>
                {
                    var tab  = (TabControl)s!;
                    var page = tab.TabPages[e.Index];
                    bool sel = tab.SelectedIndex == e.Index;
                    using var bgBrush = new SolidBrush(sel
                        ? Color.FromArgb(30, 41, 59)
                        : Color.FromArgb(15, 23, 42));
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    using var fgBrush = new SolidBrush(sel
                        ? ColorTextoPrincipal
                        : ColorTextoSecundario);
                    var sf = new StringFormat
                        { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString(page.Text, tab.Font, fgBrush, e.Bounds, sf);
                    if (sel)
                    {
                        using var linePen = new Pen(ColorAcento, 2);
                        e.Graphics.DrawLine(linePen, e.Bounds.Left, e.Bounds.Bottom - 1,
                            e.Bounds.Right, e.Bounds.Bottom - 1);
                    }
                };
            }
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

            // Badge de parámetros (cuántos y cuántos requeridos)
            int totalParams = skill.Parametros?.Count ?? 0;
            int reqParams   = skill.Parametros?.Count(p => p.Requerido) ?? 0;
            var lblParams = new Label
            {
                Text      = totalParams == 0
                            ? "🔧 sin parámetros"
                            : $"🔧 {totalParams} param · {reqParams} req",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = totalParams == 0
                            ? ColorTextoSecundario
                            : (reqParams > 0 ? ColorAmbar : ColorVerde),
                Location  = new Point(12, lblDesc.Bottom + 2),
                AutoSize  = true
            };

            // Barra de botones
            var flow = new FlowLayoutPanel
            {
                Dock      = DockStyle.Bottom,
                Height    = 32,
                Padding   = new Padding(8, 2, 0, 2),
                BackColor = Color.FromArgb(22, 33, 52)
            };

            var btnEditar  = CrearBtnCard("EDITAR",   Color.FromArgb(96, 165, 250));
            var btnAbrir   = CrearBtnCard("ABRIR",    ColorTextoSecundario);
            var btnBorrar  = CrearBtnCard("BORRAR",   ColorRojo);
            var btnToggle  = CrearBtnCard(skill.Activa ? "DESACTIVAR" : "ACTIVAR",
                                          skill.Activa ? ColorAmbar : ColorVerde);

            btnEditar.Tag  = skill;
            btnAbrir.Tag   = skill;
            btnBorrar.Tag  = skill;
            btnToggle.Tag  = skill;

            btnEditar.Click += (s, e) => CargarEnEditor((Skill)((Button)s!).Tag!);
            btnAbrir.Click  += (s, e) => AbrirEnExplorador((Skill)((Button)s!).Tag!);
            btnBorrar.Click += (s, e) => EliminarSkill((Skill)((Button)s!).Tag!);
            btnToggle.Click += (s, e) => ToggleActivarSkill((Skill)((Button)s!).Tag!);

            flow.Controls.AddRange(new Control[] { btnEditar, btnAbrir, btnToggle, btnBorrar });

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
            panel.Controls.Add(lblParams);
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

            // Resumen de parámetros: firma compacta para el usuario
            string firma = ConstruirFirmaParametros(skill);
            txtDescripcion.Text = $"Categoría: {skill.Categoria}  |  " +
                                  $"Estado: {(skill.Activa ? "Activa" : "Inactiva")}  |  " +
                                  $"ID: {skill.IdEfectivo}" +
                                  (string.IsNullOrEmpty(firma) ? "" : $"\r\nParámetros: {firma}");

            // Refrescar el editor visual de parámetros (tab 🔧 Parámetros)
            _paramsActuales = new List<SkillParametro>(
                skill.Parametros ?? new List<SkillParametro>());
            RefrescarGridParametros();

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
- nombre: ejemplo | tipo: string | requerido: false | descripcion: Parámetro de ejemplo | default: hola
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

            // Guardar antes de probar (esto re-parsea el .md, así tomamos
            // los parámetros más recientes del editor)
            btnGuardar_Click(sender, e);
            await Task.Delay(300); // dar tiempo al guardado async

            // Re-parsear desde disco para tener los parámetros actualizados
            Skill skillParaProbar = _skillActual;
            if (!string.IsNullOrWhiteSpace(_skillActual.RutaMd) && File.Exists(_skillActual.RutaMd))
            {
                var fresco = SkillMdParser.Parsear(_skillActual.RutaMd);
                if (fresco != null) skillParaProbar = fresco;
            }

            // Resolver ruta del .py
            string idSkill = skillParaProbar.IdEfectivo;
            string rutaPy  = Path.Combine(RutaSkill, "skills", $"{idSkill}.py");

            if (!File.Exists(rutaPy))
            {
                Log($"Script no encontrado: {rutaPy}", ColorRojo);
                return;
            }

            // Pedir parámetros al usuario si el skill los define
            string paramsJson = "{}";
            if (skillParaProbar.Parametros != null && skillParaProbar.Parametros.Count > 0)
            {
                var valores = DialogoParametrosSkill.MostrarYObtener(skillParaProbar, this);
                if (valores == null)
                {
                    Log("Prueba cancelada por el usuario.", ColorAmbar);
                    return;
                }
                paramsJson = valores.ToString(Newtonsoft.Json.Formatting.None);

                // Mostrar resumen en el log
                int dados = valores.Properties().Count();
                Log($"Probando con {dados} parámetro(s): {Truncar(paramsJson, 200)}", ColorAcento);
            }

            EjecutarScript("python", $"\"{rutaPy}\"", paramsJson);
        }

        private static string Truncar(string s, int n)
            => string.IsNullOrEmpty(s) || s.Length <= n ? s ?? "" : s[..n] + "…";

        /// <summary>
        /// Construye una firma legible con los parámetros del skill.
        /// Formato: "ruta: string*, sheet: string, timeout: number=30"
        /// (* = requerido; =valor = valor por defecto)
        /// </summary>
        private static string ConstruirFirmaParametros(Skill skill)
        {
            if (skill.Parametros == null || skill.Parametros.Count == 0)
                return "";

            var partes = skill.Parametros.Select(p =>
            {
                string sufijo = p.Requerido ? "*" : "";
                string defecto = !string.IsNullOrWhiteSpace(p.ValorPorDefecto)
                    ? $"={p.ValorPorDefecto}"
                    : "";
                return $"{p.Nombre}: {p.Tipo}{sufijo}{defecto}";
            });
            return string.Join(", ", partes);
        }

        private void EjecutarScript(string ejecutable, string argumentos, string skillParamsJson = "{}")
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

            // Inyectar parámetros del usuario al script (mismo contrato que el agente)
            info.Environment["SKILL_PARAMS"]    = skillParamsJson ?? "{}";
            info.Environment["SKILL_RUTA_BASE"] = RutaSkill ?? "";

            _procesoActual = new Process
            {
                StartInfo           = info,
                EnableRaisingEvents = true
            };

            // Buffer del stdout para intentar renderizar el JSON final del skill
            var sbStdout = new StringBuilder();

            _procesoActual.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    sbStdout.AppendLine(e.Data);
                    AppendOutput(e.Data + "\n", ColorTextoSecundario);
                }
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

                // Render JSON-aware del resultado, si el skill cumple el contrato
                try { SkillResultRenderer.RenderizarAlFinal(_rtbOutput, sbStdout.ToString()); }
                catch { /* render best-effort, no romper la UI por un parseo */ }

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
                    _paramsActuales.Clear();
                    RefrescarGridParametros();
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

        // ── Hub — lógica ──────────────────────────────────────────────────────

        /// <summary>Muestra u oculta el panel Hub y refresca las cards de skills remotos.</summary>
        private void AlternarPanelHub()
        {
            bool mostrar = !_pnlHub.Visible;
            _pnlHub.Location = _tabInferior.Location;
            _pnlHub.Size     = _tabInferior.Size;
            _pnlHub.Visible  = mostrar;

            if (mostrar)
            {
                _pnlHub.BringToFront();
                RefrescarCardsHub();
                _txtHubUrl.Focus();
            }
        }

        /// <summary>Rellena _flpHubCards con los skills que tienen SourceUrl.</summary>
        private void RefrescarCardsHub()
        {
            _flpHubCards.Controls.Clear();

            var deHub = _skills.FindAll(s => s.EsDeHub);

            if (deHub.Count == 0)
            {
                _flpHubCards.Controls.Add(new Label
                {
                    Text      = "Aún no hay skills instalados desde el Hub.\nPega una URL arriba e instala el primero.",
                    Font      = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    AutoSize  = true,
                    Padding   = new Padding(8)
                });
                return;
            }

            foreach (var s in deHub)
                _flpHubCards.Controls.Add(CrearCardHub(s));
        }

        /// <summary>Card compacta para un skill del Hub (con Actualizar + URL).</summary>
        private Panel CrearCardHub(Skill skill)
        {
            var card = new Panel
            {
                Width     = 240,
                Height    = 116,
                Margin    = new Padding(6),
                BackColor = Color.FromArgb(20, 30, 55),
                Cursor    = Cursors.Default
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(60, 99, 102, 241), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            // Ícono + nombre
            var lblNombre = new Label
            {
                Text      = $"📦  {skill.NombreEfectivo}",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(199, 210, 254),
                Location  = new Point(10, 10),
                Width     = 220,
                AutoEllipsis = true
            };

            // Autor + versión
            string meta = "";
            if (!string.IsNullOrWhiteSpace(skill.Autor))   meta += $"@{skill.Autor}";
            if (!string.IsNullOrWhiteSpace(skill.Version)) meta += $"  v{skill.Version}";
            var lblMeta = new Label
            {
                Text      = string.IsNullOrWhiteSpace(meta) ? "sin metadatos" : meta,
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(10, 30),
                Width     = 220,
                AutoEllipsis = true
            };

            // URL truncada
            var lblUrl = new Label
            {
                Text      = "🔗 " + TruncarUrl(skill.SourceUrl, 36),
                Font      = new Font("Consolas", 6.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location  = new Point(10, 48),
                Width     = 220,
                AutoEllipsis = true
            };
            var tt = new ToolTip();
            tt.SetToolTip(lblUrl, skill.SourceUrl);

            // Botones
            var flow = new FlowLayoutPanel
            {
                Location  = new Point(6, 74),
                Size      = new Size(228, 30),
                BackColor = Color.Transparent
            };

            var btnActualizar = CrearBtnCard("🔄 Actualizar", Color.FromArgb(52, 211, 153));
            var btnCopiarUrl  = CrearBtnCard("📋 URL",        Color.FromArgb(129, 140, 248));
            var btnEditar2    = CrearBtnCard("✏ Editar",      Color.FromArgb(148, 163, 184));

            btnActualizar.Width = 90;
            btnCopiarUrl.Width  = 60;
            btnEditar2.Width    = 60;

            btnActualizar.Click += async (_, _) => await ActualizarSkillHubAsync(skill);
            btnCopiarUrl.Click  += (_, _) =>
            {
                try { Clipboard.SetText(skill.SourceUrl); Log("URL copiada al portapapeles.", ColorVerde); }
                catch { }
            };
            btnEditar2.Click += (_, _) =>
            {
                AlternarPanelHub(); // cerrar hub
                CargarEnEditor(skill);
            };

            flow.Controls.AddRange(new Control[] { btnActualizar, btnCopiarUrl, btnEditar2 });
            card.Controls.AddRange(new Control[] { lblNombre, lblMeta, lblUrl, flow });
            return card;
        }

        /// <summary>Instala un skill desde la URL ingresada en _txtHubUrl.</summary>
        private async void BtnHubInstalar_Click(object sender, EventArgs e)
        {
            string url = _txtHubUrl.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                EstadoHub("Pega una URL primero.", ColorAmbar);
                return;
            }

            _ctsHub?.Cancel();
            _ctsHub = new CancellationTokenSource();

            _btnHubInstalar.Enabled = false;
            _btnHubInstalar.Text    = "⟳ Instalando...";
            EstadoHub("Descargando…", Color.FromArgb(129, 140, 248));

            try
            {
                var skill = await SkillHubManager.InstalarDesdeUrlAsync(
                    url, RutaSkill, _ctsHub.Token);

                // Regenerar skill_runner.py con todos los skills
                var todos = SkillLoader.CargarTodas(RutaSkill);
                await SkillRunnerHelper.GenerarAsync(RutaSkill, todos);

                _txtHubUrl.Clear();
                EstadoHub($"✔ Instalado: {skill.NombreEfectivo}", ColorVerde);
                Log($"Hub: skill instalado — {skill.NombreEfectivo} ({skill.IdEfectivo})", ColorVerde);

                // Refrescar UI
                CargarDatos();
                RefrescarCardsHub();
            }
            catch (SkillHubException ex)
            {
                EstadoHub($"✖ {ex.Message}", ColorRojo);
                Log($"Hub: {ex.Message}", ColorRojo);
            }
            catch (OperationCanceledException)
            {
                EstadoHub("Cancelado.", ColorAmbar);
            }
            catch (Exception ex)
            {
                EstadoHub($"✖ Error inesperado: {ex.Message}", ColorRojo);
            }
            finally
            {
                _btnHubInstalar.Enabled = true;
                _btnHubInstalar.Text    = "📥 Instalar";
            }
        }

        /// <summary>Re-descarga y actualiza un skill del Hub desde su SourceUrl.</summary>
        private async Task ActualizarSkillHubAsync(Skill skill)
        {
            EstadoHub($"Actualizando {skill.NombreEfectivo}…", Color.FromArgb(129, 140, 248));
            try
            {
                var actualizado = await SkillHubManager.ActualizarAsync(skill, RutaSkill);
                var todos = SkillLoader.CargarTodas(RutaSkill);
                await SkillRunnerHelper.GenerarAsync(RutaSkill, todos);

                EstadoHub($"✔ Actualizado: {actualizado.NombreEfectivo}", ColorVerde);
                Log($"Hub: actualizado — {actualizado.NombreEfectivo}", ColorVerde);

                CargarDatos();
                RefrescarCardsHub();
            }
            catch (SkillHubException ex)
            {
                EstadoHub($"✖ {ex.Message}", ColorRojo);
                Log($"Hub: {ex.Message}", ColorRojo);
            }
            catch (Exception ex)
            {
                EstadoHub($"✖ {ex.Message}", ColorRojo);
            }
        }

        /// <summary>Copia el .md completo del skill actual al portapapeles.</summary>
        private void BtnExportarSkill_Click(object sender, EventArgs e)
        {
            if (_skillActual == null)
            {
                Log("Selecciona un skill en el editor antes de exportar.", ColorAmbar);
                return;
            }

            string md = SkillHubManager.GenerarMdParaExportar(_skillActual);
            if (string.IsNullOrWhiteSpace(md))
            {
                Log("El skill no tiene archivo .md para exportar.", ColorAmbar);
                return;
            }

            try
            {
                Clipboard.SetText(md);
                Log($"📋 .md de '{_skillActual.NombreEfectivo}' copiado al portapapeles.", ColorVerde);
            }
            catch (Exception ex)
            {
                Log($"Error al copiar: {ex.Message}", ColorRojo);
            }
        }

        /// <summary>Actualiza el label de estado del Hub (thread-safe).</summary>
        private void EstadoHub(string texto, Color color)
        {
            if (InvokeRequired) { Invoke(() => EstadoHub(texto, color)); return; }
            _lblHubEstado.Text      = texto;
            _lblHubEstado.ForeColor = color;
        }

        private static string TruncarUrl(string url, int max)
            => url.Length <= max ? url : "…" + url[^(max - 1)..];

        // ── Toggle activar / desactivar skill ────────────────────────────────

        /// <summary>
        /// Invierte el campo activa: en el frontmatter del .md y recarga la vista.
        /// Si el skill no tiene .md (legado JSON) avisa al usuario.
        /// </summary>
        private void ToggleActivarSkill(Skill skill)
        {
            if (string.IsNullOrWhiteSpace(skill.RutaMd) || !File.Exists(skill.RutaMd))
            {
                Log("Este skill no tiene archivo .md — edítalo manualmente.", ColorAmbar);
                return;
            }

            try
            {
                string contenido = File.ReadAllText(skill.RutaMd, Encoding.UTF8);
                bool ahora       = skill.Activa;
                bool nuevo       = !ahora;

                // Sustituir dentro del frontmatter la línea activa: true/false
                string reemplazado = System.Text.RegularExpressions.Regex.Replace(
                    contenido,
                    @"(?m)^(activa\s*:\s*)(true|false)\s*$",
                    $"${{1}}{nuevo.ToString().ToLower()}");

                // Si no existía el campo, lo inyectamos al final del frontmatter
                if (reemplazado == contenido)
                {
                    int cierreFront = contenido.IndexOf("---", 3);
                    if (cierreFront > 0)
                        reemplazado = contenido.Insert(cierreFront,
                            $"activa: {nuevo.ToString().ToLower()}\n");
                }

                File.WriteAllText(skill.RutaMd, reemplazado, Encoding.UTF8);
                skill.Activa = nuevo;

                string estado = nuevo ? "activado ✔" : "desactivado ✗";
                Log($"Skill '{skill.NombreEfectivo}' {estado}", nuevo ? ColorVerde : ColorAmbar);
                CargarDatos();
            }
            catch (Exception ex)
            {
                Log($"Error al cambiar estado: {ex.Message}", ColorRojo);
            }
        }

        // ── Panel Agente Creador ──────────────────────────────────────────────

        private void ConstruirPanelCreador()
        {
            _pnlCreador = new Panel
            {
                Location  = pnlContenedor.Location,
                Size      = pnlContenedor.Size,
                BackColor = Color.FromArgb(5, 12, 24),
                Visible   = false,
                Name      = "pnlCreador"
            };
            _pnlCreador.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(52, 211, 153), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _pnlCreador.Width - 1, _pnlCreador.Height - 1);
            };

            // ── Encabezado ────────────────────────────────────────────────────
            var lblTitulo = new Label
            {
                Text      = "🤖  AGENTE CREADOR DE SKILLS",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(167, 243, 208),
                Location  = new Point(16, 14),
                AutoSize  = true
            };

            var btnCerrar = new Button
            {
                Text      = "✕",
                Size      = new Size(28, 24),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Location = new Point(_pnlCreador.Width - 38, 10);
            btnCerrar.Click   += (_, _) => AlternarPanelCreador();
            _pnlCreador.Resize += (_, _) => btnCerrar.Left = _pnlCreador.Width - 38;

            var sep1 = new Panel
            {
                Location  = new Point(0, 44),
                Size      = new Size(_pnlCreador.Width, 1),
                BackColor = Color.FromArgb(30, 52, 211, 153),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlCreador.Resize += (_, _) => sep1.Width = _pnlCreador.Width;

            // ── Descripción ───────────────────────────────────────────────────
            var lblDesc = new Label
            {
                Text      = "¿Qué debe hacer el skill?",
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location  = new Point(16, 56),
                AutoSize  = true
            };
            var lblHint = new Label
            {
                Text      = "Describe con detalle: qué hace, qué parámetros necesita y qué debe devolver.",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location  = new Point(16, 74),
                AutoSize  = true
            };

            _txtDescripcion = new TextBox
            {
                Location    = new Point(16, 92),
                Size        = new Size(_pnlCreador.Width - 32, 56),
                BackColor   = Color.FromArgb(15, 23, 42),
                ForeColor   = Color.FromArgb(226, 232, 240),
                Font        = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                PlaceholderText = "Ej: \"Un skill que consulte el precio de Bitcoin en tiempo real\"",
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlCreador.Resize += (_, _) => _txtDescripcion.Width = _pnlCreador.Width - 32;

            // ── Botones de acción ─────────────────────────────────────────────
            _btnCrearIAStart = new Button
            {
                Text      = "⚡ Generar Skill",
                Location  = new Point(16, 158),
                Size      = new Size(130, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(5, 12, 24),
                BackColor = Color.FromArgb(52, 211, 153),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            _btnCrearIAStart.FlatAppearance.BorderSize = 0;
            _btnCrearIAStart.Click += BtnCrearIAStart_Click;

            var btnCancelarCreador = new Button
            {
                Text      = "⏹ Cancelar",
                Location  = new Point(156, 158),
                Size      = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(248, 113, 113),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCancelarCreador.FlatAppearance.BorderColor = Color.FromArgb(248, 113, 113);
            btnCancelarCreador.FlatAppearance.BorderSize  = 1;
            btnCancelarCreador.Click += (_, _) => _ctsCreador?.Cancel();

            var btnLimpiar = new Button
            {
                Text      = "🗑 Limpiar",
                Location  = new Point(256, 158),
                Size      = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Color.Transparent,
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnLimpiar.FlatAppearance.BorderSize  = 1;
            btnLimpiar.Click += (_, _) => { _rtbCreadorLog.Clear(); EstadoCreador("Log limpiado.", Color.FromArgb(100, 116, 139)); };

            // ── Estado + fase actual ──────────────────────────────────────────
            _lblCreadorEstado = new Label
            {
                Text      = "Describe el skill y pulsa ⚡ Generar.",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(350, 165),
                Size      = new Size(_pnlCreador.Width - 365, 18),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlCreador.Resize += (_, _) => _lblCreadorEstado.Width = _pnlCreador.Width - 365;

            // ── Indicador de fases ────────────────────────────────────────────
            _pnlFases = new Panel
            {
                Location  = new Point(8, 196),
                Size      = new Size(_pnlCreador.Width - 16, 28),
                BackColor = Color.FromArgb(10, 20, 38),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlCreador.Resize += (_, _) => _pnlFases.Width = _pnlCreador.Width - 16;

            string[] fasesNombres = { "1 Generar", "2 Parsear", "3 Probar", "4 Finalizar" };
            int fasW = (_pnlCreador.Width - 16) / 4;
            for (int i = 0; i < fasesNombres.Length; i++)
            {
                int idx = i;
                var lblF = new Label
                {
                    Text      = fasesNombres[i],
                    Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location  = new Point(idx * fasW, 0),
                    Size      = new Size(fasW, 28),
                    Name      = $"lblFase{idx + 1}"
                };
                _pnlFases.Controls.Add(lblF);
            }
            _pnlFases.Resize += (_, _) =>
            {
                int w = _pnlFases.Width / 4;
                for (int i = 0; i < _pnlFases.Controls.Count; i++)
                    _pnlFases.Controls[i].SetBounds(i * w, 0, w, 28);
            };

            _lblFaseActual = new Label { Text = "", AutoSize = false, Visible = false };

            // ── ProgressBar ───────────────────────────────────────────────────
            _pbCreador = new ProgressBar
            {
                Location = new Point(8, 226),
                Size     = new Size(_pnlCreador.Width - 16, 6),
                Minimum  = 0,
                Maximum  = 100,
                Value    = 0,
                Style    = ProgressBarStyle.Continuous,
                Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlCreador.Resize += (_, _) => _pbCreador.Width = _pnlCreador.Width - 16;

            // ── Separador + Log ───────────────────────────────────────────────
            var sep2 = new Panel
            {
                Location  = new Point(0, 234),
                Size      = new Size(_pnlCreador.Width, 1),
                BackColor = Color.FromArgb(20, 52, 211, 153),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlCreador.Resize += (_, _) => sep2.Width = _pnlCreador.Width;

            var lblLog = new Label
            {
                Text      = "▸ Log del agente",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                Location  = new Point(16, 242),
                AutoSize  = true
            };

            _rtbCreadorLog = new RichTextBox
            {
                Location    = new Point(8, 260),
                Size        = new Size(_pnlCreador.Width - 16, _pnlCreador.Height - 268),
                BackColor   = Color.FromArgb(2, 6, 18),
                ForeColor   = Color.FromArgb(100, 116, 139),
                Font        = new Font("Consolas", 8.5f),
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                WordWrap    = true,
                Anchor      = AnchorStyles.Top | AnchorStyles.Bottom |
                              AnchorStyles.Left | AnchorStyles.Right
            };

            _pnlCreador.Controls.AddRange(new Control[]
            {
                lblTitulo, btnCerrar, sep1,
                lblDesc, lblHint, _txtDescripcion,
                _btnCrearIAStart, btnCancelarCreador, btnLimpiar,
                _lblCreadorEstado, _lblFaseActual,
                _pnlFases, _pbCreador,
                sep2, lblLog, _rtbCreadorLog
            });

            Controls.Add(_pnlCreador);
            _pnlCreador.BringToFront();
        }

        /// <summary>Marca visualmente la fase activa en el indicador de pasos.</summary>
        private void MarcarFaseActiva(int fase) // 1..4
        {
            if (_pnlFases == null || !_pnlFases.IsHandleCreated) return;
            _pnlFases.Invoke(() =>
            {
                foreach (Control c in _pnlFases.Controls)
                {
                    if (c is not Label lbl) continue;
                    int n = int.TryParse(c.Name.Replace("lblFase", ""), out int v) ? v : 0;
                    if (n == fase)
                    {
                        lbl.ForeColor = Color.FromArgb(52, 211, 153);
                        lbl.Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold | FontStyle.Underline);
                    }
                    else if (n < fase)
                    {
                        lbl.ForeColor = Color.FromArgb(71, 120, 100);
                        lbl.Font      = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                    }
                    else
                    {
                        lbl.ForeColor = Color.FromArgb(51, 65, 85);
                        lbl.Font      = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                    }
                }
            });
        }

        /// <summary>Actualiza el ProgressBar de forma segura.</summary>
        private void AvanzarProgreso(int porcentaje)
        {
            if (_pbCreador == null || !_pbCreador.IsHandleCreated) return;
            _pbCreador.Invoke(() => _pbCreador.Value = Math.Clamp(porcentaje, 0, 100));
        }

        private void AlternarPanelCreador()
        {
            bool mostrar = !_pnlCreador.Visible;
            _pnlCreador.Location = _tabInferior.Location;
            _pnlCreador.Size     = _tabInferior.Size;
            _pnlCreador.Visible  = mostrar;
            if (mostrar)
            {
                if (_pnlHub.Visible) _pnlHub.Visible = false;
                _pnlCreador.BringToFront();
                _txtDescripcion.Focus();
            }
        }

        // ── Agente Creador — lógica central ───────────────────────────────────

        private async void BtnCrearIAStart_Click(object sender, EventArgs e)
        {
            string desc = _txtDescripcion.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
            {
                EstadoCreador("Escribe una descripción primero.", ColorAmbar);
                return;
            }
            bool requiereApiKey = Agente != Servicios.Ollama;
            if (string.IsNullOrWhiteSpace(Modelo) || (requiereApiKey && string.IsNullOrWhiteSpace(ApiKey)))
            {
                EstadoCreador("Sin modelo/API key configurados.", ColorRojo);
                return;
            }

            _ctsCreador?.Cancel();
            _ctsCreador = new CancellationTokenSource();
            var ct      = _ctsCreador.Token;

            _btnCrearIAStart.Enabled = false;
            _btnCrearIAStart.Text    = "⟳ Generando...";
            _rtbCreadorLog.Clear();
            AvanzarProgreso(0);
            MarcarFaseActiva(1);

            try
            {
                await EjecutarAgenteCreadorAsync(desc, ct);
            }
            catch (OperationCanceledException)
            {
                LogCreador("⏹ Cancelado por el usuario.", ColorAmbar);
                EstadoCreador("Cancelado.", ColorAmbar);
            }
            catch (Exception ex)
            {
                LogCreador($"✖ Error inesperado: {ex.Message}", ColorRojo);
                EstadoCreador("Error.", ColorRojo);
            }
            finally
            {
                _btnCrearIAStart.Enabled = true;
                _btnCrearIAStart.Text    = "⚡ Generar Skill";
            }
        }

        /// <summary>
        /// Núcleo del agente creador — 4 fases con agente recuperador integrado:
        ///  1. Genera el .md completo con LLM
        ///  2. Parsea, guarda y extrae .py (+ agente recuperador si falla)
        ///  3. Ejecuta y verifica output; corrige con LLM hasta MAX_INTENTOS
        ///  4. Resultado final + recarga de la lista
        /// </summary>
        private async Task EjecutarAgenteCreadorAsync(string descripcion, CancellationToken ct)
        {
            const int MAX_INTENTOS = 3;
            string carpeta = Path.Combine(RutaSkill, "skills");
            Directory.CreateDirectory(carpeta);

            AvanzarProgreso(0);
            MarcarFaseActiva(1);

            // ── FASE 1: Generar el .md ────────────────────────────────────────
            LogCreador("━━ FASE 1/4 — Generando skill con IA ━━", Color.FromArgb(167, 243, 208));
            EstadoCreador("Consultando al agente IA...", Color.FromArgb(129, 140, 248));
            AvanzarProgreso(5);

            var ctx = await AgentContext.BuildAsync(
                RutaSkill, Modelo, ApiKey, Agente,
                soloChat: false, ClavesApis, ct);

            string promptSistema = ConstruirPromptSistemaCreador(carpeta);
            var ctxGen = ctx.ConPromptPersonalizado(promptSistema);

            string respuesta = await AIModelConector.ObtenerRespuestaLLMAsync(
                $"Crea un skill Python para: {descripcion}", ctxGen, ct);

            LogCreador($"  ✔ Respuesta recibida ({respuesta.Length:N0} caracteres)", ColorVerde);
            AvanzarProgreso(20);

            // ── FASE 2: Parsear y guardar el .md ─────────────────────────────
            MarcarFaseActiva(2);
            LogCreador("\n━━ FASE 2/4 — Parseando y guardando ━━", Color.FromArgb(167, 243, 208));
            EstadoCreador("Extrayendo .md...", Color.FromArgb(129, 140, 248));

            string mdContenido = ExtraerContenidoMd(respuesta);

            if (string.IsNullOrWhiteSpace(mdContenido))
            {
                LogCreador("  ⚠ No se detectó bloque .md. Reintentando con instrucción explícita...", ColorAmbar);
                respuesta = await AIModelConector.ObtenerRespuestaLLMAsync(
                    "DEVUELVE SOLO el archivo .md completo, comenzando con --- (frontmatter) " +
                    "hasta el final de la sección ## Parametros. Sin texto adicional. " +
                    $"Skill: {descripcion}", ctxGen, ct);
                mdContenido = ExtraerContenidoMd(respuesta);
            }

            if (string.IsNullOrWhiteSpace(mdContenido))
                throw new Exception("No se pudo extraer el .md de la respuesta del LLM.");

            // Diagnóstico: ¿tiene sección Codigo?
            bool tieneCodigo = mdContenido.Contains("## Codigo", StringComparison.OrdinalIgnoreCase)
                            || mdContenido.Contains("## Código", StringComparison.OrdinalIgnoreCase);
            bool tienePython = mdContenido.Contains("```python", StringComparison.OrdinalIgnoreCase);
            LogCreador($"  ℹ .md extraído: {mdContenido.Length:N0} chars | " +
                       $"§Codigo: {(tieneCodigo ? "✔" : "✗")} | ` ```python `: {(tienePython ? "✔" : "✗")}",
                       Color.FromArgb(100, 116, 139));

            // Parsear para obtener ID y ContenidoMd
            string tmpPath = Path.Combine(Path.GetTempPath(), $"skill_tmp_{Guid.NewGuid():N}.md");
            File.WriteAllText(tmpPath, mdContenido, Encoding.UTF8);
            var skillParsed = SkillMdParser.Parsear(tmpPath);
            try { File.Delete(tmpPath); } catch { }

            if (skillParsed == null)
                throw new Exception("El .md generado no tiene frontmatter válido (falta id/nombre).");

            LogCreador($"  ✔ Skill ID: '{skillParsed.IdEfectivo}' | Nombre: '{skillParsed.NombreEfectivo}'", ColorVerde);

            string rutaMd = Path.Combine(carpeta, $"{skillParsed.IdEfectivo}.md");
            File.WriteAllText(rutaMd, mdContenido, Encoding.UTF8);
            LogCreador($"  ✔ Guardado: {skillParsed.IdEfectivo}.md", ColorVerde);
            AvanzarProgreso(35);

            // Intentar extraer .py del .md
            await SkillRunnerHelper.ExtraerScriptsMdAsync(RutaSkill, new[] { skillParsed }, ct);
            string rutaPy = Path.Combine(carpeta, $"{skillParsed.IdEfectivo}.py");

            // ── AGENTE RECUPERADOR: si el .py no se generó ───────────────────
            if (!File.Exists(rutaPy))
            {
                LogCreador("\n  ⚠ El .py no se generó desde el .md — activando agente recuperador...", ColorAmbar);
                EstadoCreador("Agente recuperador activo...", ColorAmbar);

                // Estrategia 1: extraer código desde ContenidoMd en memoria
                string? codigoRec = SkillMdParser.ExtraerCodigo(skillParsed.ContenidoMd ?? "");
                if (!string.IsNullOrWhiteSpace(codigoRec))
                {
                    File.WriteAllText(rutaPy, codigoRec, Encoding.UTF8);
                    LogCreador($"  ↻ Recuperado (estrategia 1 — ContenidoMd): {codigoRec.Split('\n').Length} líneas", ColorAmbar);
                }

                // Estrategia 2: extraer código Python directo de la respuesta LLM raw
                if (!File.Exists(rutaPy))
                {
                    codigoRec = ExtraerCodigoPython(respuesta);
                    if (!string.IsNullOrWhiteSpace(codigoRec))
                    {
                        File.WriteAllText(rutaPy, codigoRec, Encoding.UTF8);
                        LogCreador($"  ↻ Recuperado (estrategia 2 — respuesta LLM raw): {codigoRec.Split('\n').Length} líneas", ColorAmbar);
                    }
                }

                // Estrategia 3: pedir al LLM solo el código Python
                if (!File.Exists(rutaPy))
                {
                    LogCreador("  🤖 Estrategia 3 — solicitando código Python al agente IA...", ColorAmbar);
                    var ctxRec     = ctx.ConPromptPersonalizado(ConstruirPromptCorrector(carpeta));
                    string respRec = await AIModelConector.ObtenerRespuestaLLMAsync(
                        $"El siguiente skill fue generado pero su código Python no pudo ser extraído.\n\n" +
                        $"Skill ID: {skillParsed.IdEfectivo}\n" +
                        $"Descripción: {descripcion}\n\n" +
                        $"Devuelve SOLO el código Python completo y funcional (sin ```, sin markdown). " +
                        $"Debe leer params con os.environ.get('SKILL_PARAMS','{{}}') y " +
                        $"terminar con print(json.dumps(resultado)).",
                        ctxRec, ct);
                    codigoRec = ExtraerCodigoPython(respRec);
                    if (!string.IsNullOrWhiteSpace(codigoRec))
                    {
                        File.WriteAllText(rutaPy, codigoRec, Encoding.UTF8);
                        LogCreador($"  ↻ Recuperado (estrategia 3 — agente IA): {codigoRec.Split('\n').Length} líneas", ColorAmbar);
                    }
                }

                if (!File.Exists(rutaPy))
                    throw new Exception(
                        $"No se pudo generar el .py para '{skillParsed.IdEfectivo}' " +
                        $"con ninguna estrategia. Verifica que el LLM esté devolviendo código Python válido.");
            }

            long pyBytes = new FileInfo(rutaPy).Length;
            LogCreador($"  ✔ Script listo: {skillParsed.IdEfectivo}.py ({pyBytes / 1024.0:F1} KB)", ColorVerde);
            AvanzarProgreso(50);

            // ── Fin de Fase 2: cargar el skill recién generado en el editor ──
            // Así el usuario ve el código + los parámetros parseados en el tab
            // 🔧 Parámetros antes de que arranque la prueba.
            int totalParams = skillParsed.Parametros?.Count ?? 0;
            int reqParams   = skillParsed.Parametros?.Count(p => p.Requerido) ?? 0;
            LogCreador(
                $"  ℹ Parámetros detectados: {totalParams} ({reqParams} requeridos)",
                Color.FromArgb(100, 116, 139));

            if (IsHandleCreated)
            {
                Invoke(() =>
                {
                    CargarDatos();
                    var refrescado = SkillMdParser.Parsear(rutaMd) ?? skillParsed;
                    CargarEnEditor(refrescado);
                });
            }

            // ── FASE 3: Prueba iterativa con corrección ───────────────────────
            MarcarFaseActiva(3);
            LogCreador("\n━━ FASE 3/4 — Probando el skill ━━", Color.FromArgb(167, 243, 208));

            // Si el skill define parámetros requeridos, pedirlos al usuario
            // ANTES de la primera ejecución. Sin esto, fase 3 fallaría siempre
            // con KeyError aunque el código sea correcto.
            string paramsPrueba = "{}";
            if (totalParams > 0)
            {
                EstadoCreador("Esperando valores de prueba...", Color.FromArgb(129, 140, 248));
                LogCreador(
                    "  ⏸ Pidiendo valores de prueba al usuario para los parámetros...",
                    Color.FromArgb(165, 180, 252));

                JObject? valores = null;
                if (IsHandleCreated)
                {
                    Invoke(() =>
                    {
                        valores = DialogoParametrosSkill.MostrarYObtener(skillParsed, this);
                    });
                }

                if (valores == null)
                {
                    // Usuario canceló — saltamos fase 3 pero conservamos el skill
                    LogCreador(
                        "  ⏭ Prueba omitida por el usuario. El skill se guardó sin verificar.",
                        ColorAmbar);
                    EstadoCreador("⏭ Prueba omitida — skill guardado", ColorAmbar);
                    MarcarFaseActiva(4);
                    AvanzarProgreso(95);
                    var todosSinProbar = SkillLoader.CargarTodas(RutaSkill);
                    await SkillRunnerHelper.GenerarAsync(RutaSkill, todosSinProbar);
                    LogCreador($"", Color.White);
                    LogCreador($"  ⚠ SKILL GUARDADO SIN PROBAR", ColorAmbar);
                    LogCreador($"  Archivo: {rutaMd}", Color.FromArgb(71, 85, 105));
                    LogCreador(
                        "  Pulsa 'Probar' en el editor cuando quieras ejecutarlo.",
                        ColorAmbar);
                    if (IsHandleCreated) Invoke(() => CargarDatos());
                    return;
                }

                paramsPrueba = valores.ToString(Newtonsoft.Json.Formatting.None);
                LogCreador(
                    $"  ✔ Valores capturados: {Truncar(paramsPrueba, 200)}",
                    Color.FromArgb(100, 200, 155));
            }

            bool exitoso   = false;
            string? stdout = null;
            string? stderr = null;
            int progBase   = 50;

            for (int intento = 1; intento <= MAX_INTENTOS; intento++)
            {
                ct.ThrowIfCancellationRequested();
                EstadoCreador($"Ejecutando prueba {intento}/{MAX_INTENTOS}...", Color.FromArgb(251, 191, 36));
                LogCreador($"\n  ▶ Ejecución {intento}/{MAX_INTENTOS}", Color.FromArgb(148, 163, 184));
                AvanzarProgreso(progBase + (intento - 1) * 12);

                (stdout, stderr) = await EjecutarPythonAsync(rutaPy, ct, paramsPrueba, RutaSkill);

                // Mostrar salida recortada
                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    string prev = stdout.Length > 300 ? stdout[..300] + "…" : stdout;
                    foreach (var l in prev.Split('\n').Take(6))
                        LogCreador($"    {l}", Color.FromArgb(80, 160, 130));
                }
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    string prevErr = stderr.Length > 200 ? stderr[..200] + "…" : stderr;
                    foreach (var l in prevErr.Split('\n').Take(5))
                        LogCreador($"    ⚠ {l}", Color.FromArgb(248, 113, 113));
                }

                string? falloSemantico = ValidarOutputSkill(stdout, stderr);

                if (falloSemantico == null)
                {
                    exitoso = true;
                    LogCreador($"  ✔ Prueba {intento} — output válido ✅", ColorVerde);
                    break;
                }

                // ── CORRECCIÓN ────────────────────────────────────────────────
                LogCreador($"  ✖ Prueba {intento} falló: {falloSemantico}", ColorRojo);

                if (intento == MAX_INTENTOS) break;

                LogCreador($"  ✏ Corrección con IA (intento {intento}/{MAX_INTENTOS - 1})...", ColorAmbar);
                EstadoCreador($"Corrigiendo ({intento}/{MAX_INTENTOS - 1})...", ColorAmbar);

                string codigoActual = File.ReadAllText(rutaPy, Encoding.UTF8);
                string error        = !string.IsNullOrWhiteSpace(stderr) ? stderr : falloSemantico;

                var    ctxFix    = ctx.ConPromptPersonalizado(ConstruirPromptCorrector(carpeta));
                string promptFix = ConstruirPromptCorreccion(
                    descripcion, skillParsed.IdEfectivo, codigoActual, error, falloSemantico);

                string respCorr  = await AIModelConector.ObtenerRespuestaLLMAsync(promptFix, ctxFix, ct);
                string codigoCorr = ExtraerCodigoPython(respCorr);

                if (!string.IsNullOrWhiteSpace(codigoCorr))
                {
                    File.WriteAllText(rutaPy, codigoCorr, Encoding.UTF8);
                    LogCreador($"  ↻ Código corregido ({codigoCorr.Split('\n').Length} líneas). Volviendo a probar...", ColorAmbar);
                }
                else
                {
                    LogCreador("  ⚠ El corrector no devolvió código nuevo.", ColorAmbar);
                }
            }

            // ── FASE 4: Resultado final ───────────────────────────────────────
            MarcarFaseActiva(4);
            AvanzarProgreso(95);
            LogCreador("\n━━ FASE 4/4 — RESULTADO FINAL ━━", Color.FromArgb(167, 243, 208));

            var todos = SkillLoader.CargarTodas(RutaSkill);
            await SkillRunnerHelper.GenerarAsync(RutaSkill, todos);

            if (exitoso)
            {
                AvanzarProgreso(100);
                LogCreador($"", Color.White);
                LogCreador($"  ✅ SKILL CREADO Y FUNCIONANDO", ColorVerde);
                LogCreador($"  ─────────────────────────────────────────", Color.FromArgb(30, 80, 60));
                LogCreador($"  Nombre:  {skillParsed.NombreEfectivo}", ColorVerde);
                LogCreador($"  ID:      {skillParsed.IdEfectivo}", Color.FromArgb(100, 200, 155));
                LogCreador($"  Archivo: {rutaMd}", Color.FromArgb(71, 85, 105));
                EstadoCreador($"✅ {skillParsed.NombreEfectivo} listo", ColorVerde);
            }
            else
            {
                AvanzarProgreso(85);
                LogCreador($"", Color.White);
                LogCreador($"  ⚠ SKILL GUARDADO — REQUIERE REVISIÓN", ColorAmbar);
                LogCreador($"  Se agotaron {MAX_INTENTOS} intentos de corrección.", ColorAmbar);
                LogCreador($"  El skill se guardó en: {rutaMd}", Color.FromArgb(71, 85, 105));
                LogCreador($"  Puedes editarlo manualmente con el botón EDITAR.", ColorAmbar);
                EstadoCreador("⚠ Guardado, revisar manualmente.", ColorAmbar);
            }

            CargarDatos();
        }

        // ── Helpers del agente creador ────────────────────────────────────────

        /// <summary>Prompt de sistema para el agente generador de skills.</summary>
        private string ConstruirPromptSistemaCreador(string carpeta)
        {
            string rutaApis = RutasProyecto.ObtenerRutaListApis();
            string creds    = ConstruirDetalleCredenciales();
            return $@"Eres el AGENTE CREADOR DE SKILLS de OPENGIOAI.

Tu tarea: generar un archivo .md completo y listo para usar que describa
un skill Python ejecutable.

CARPETA DE TRABAJO: {carpeta}
ARCHIVO DE CREDENCIALES: {rutaApis}
CREDENCIALES DISPONIBLES:
{creds}

FORMATO OBLIGATORIO DEL .md (respeta exactamente esta estructura):
---
id: nombre_skill_snake_case
nombre: Nombre Legible del Skill
categoria: sistema|archivos|ia|web|datos|general
descripcion: Descripcion breve de una linea
activa: true
autor: opengio
version: 1.0.0
ejemplo: skill_run(""id_skill"", param=""valor"")
---

## Descripcion
Descripcion detallada de lo que hace el skill.

## Codigo
```python
import os, sys, json, time
from datetime import datetime, timezone

def main():
    inicio = time.time()
    params = json.loads(os.environ.get(""SKILL_PARAMS"", ""{{}}""))

    try:
        # Logica principal aqui
        resultado = {{
            ""status"": ""ok"",
            ""timestamp"": datetime.now(timezone.utc).isoformat(),
            ""duracion"": round(time.time() - inicio, 3),
            ""resumen"": ""Descripcion del resultado"",
            # ... otros campos
        }}
    except Exception as e:
        resultado = {{""status"": ""error"", ""detalle"": str(e),
                     ""timestamp"": datetime.now(timezone.utc).isoformat()}}

    print(json.dumps(resultado, ensure_ascii=False, indent=2))

if __name__ == ""__main__"":
    main()
```

## Parametros
- nombre: param1 | tipo: string | requerido: true | descripcion: Que hace este parametro
- nombre: param2 | tipo: number | requerido: false | descripcion: ... | default: 30
- nombre: formato | tipo: string | requerido: false | descripcion: ... | opciones: csv, json, yaml | default: json

REGLAS ABSOLUTAS:
1. Devuelve SOLO el bloque .md — sin texto antes ni después, sin explicaciones
2. El Python SIEMPRE hace print(json.dumps(...)) al final — NUNCA devuelve vacío
3. Instala dependencias con subprocess.check_call si son necesarias
4. Lee params con: json.loads(os.environ.get(""SKILL_PARAMS"", ""{{}}""))
5. Maneja errores y los incluye en el JSON de salida con status:error
6. Lee credenciales de {rutaApis} si necesita APIs
7. El resultado siempre tiene: status, timestamp, duracion, resumen
8. Sección ## Parametros: incluye TODOS los parámetros que el código realmente usa.
   Tipos válidos: string, number, integer, boolean, array, object.
   Campos por línea: nombre, tipo, requerido (true/false), descripcion (corta),
   default (opcional, valor que se usa si el caller no lo provee), y
   opciones (opcional, lista cerrada de valores aceptados separados por coma).
9. Marca como requerido:true SOLO los parámetros sin los que el skill no puede ejecutar.
   Para los demás, define default: y márcalos requerido:false";
        }

        /// <summary>Prompt de sistema para el agente corrector.</summary>
        private string ConstruirPromptCorrector(string carpeta) => $@"
Eres el AGENTE CORRECTOR DE SKILLS. Recibes un script Python que falló
y debes devolver el código Python CORREGIDO y FUNCIONAL.

REGLAS:
1. Devuelve SOLO el código Python corregido — sin markdown, sin ```, sin texto
2. El script SIEMPRE debe hacer print(json.dumps(resultado)) al final
3. Nunca devuelvas un script que pueda terminar sin output
4. Instala dependencias si faltan con subprocess.check_call
5. Maneja TODOS los errores con try/except
6. El resultado JSON debe tener: status, timestamp, duracion, resumen
CARPETA: {carpeta}";

        /// <summary>Prompt de usuario para la corrección.</summary>
        private static string ConstruirPromptCorreccion(
            string descripcion, string skillId, string codigo, string error, string falloSemantico)
            => $@"El skill '{skillId}' falló. Corrígelo.

TAREA ORIGINAL: {descripcion}

PROBLEMA: {falloSemantico}

ERROR / STDERR:
{(error.Length > 800 ? error[..800] + "..." : error)}

CÓDIGO ACTUAL:
{(codigo.Length > 2000 ? codigo[..2000] + "\n# ... truncado" : codigo)}

Devuelve SOLO el Python corregido, completo y funcional.";

        /// <summary>
        /// Ejecuta un script Python y captura stdout y stderr.
        /// </summary>
        private static async Task<(string stdout, string stderr)> EjecutarPythonAsync(
            string rutaPy, CancellationToken ct, string paramsJson = "{}", string rutaBase = "")
        {
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            var info = new ProcessStartInfo
            {
                FileName               = "python",
                Arguments              = $"\"{rutaPy}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding  = Encoding.UTF8
            };

            info.Environment["SKILL_PARAMS"]    = paramsJson ?? "{}";
            info.Environment["SKILL_RUTA_BASE"] = rutaBase ?? "";

            using var proc = new Process { StartInfo = info, EnableRaisingEvents = true };
            var tcsExit    = new TaskCompletionSource<int>();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };
            proc.Exited             += (_, _) => tcsExit.TrySetResult(proc.ExitCode);

            using var reg = ct.Register(() => { try { proc.Kill(); } catch { } });

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await Task.WhenAny(tcsExit.Task, Task.Delay(30_000, ct));

            if (!proc.HasExited) { proc.Kill(); }

            return (sbOut.ToString().Trim(), sbErr.ToString().Trim());
        }

        /// <summary>
        /// Valida semánticamente el output de un skill.
        /// Devuelve null si es OK, o descripción del problema.
        /// </summary>
        private static string? ValidarOutputSkill(string? stdout, string? stderr)
        {
            // Si hay stderr con excepción Python → fallo técnico
            if (!string.IsNullOrWhiteSpace(stderr) &&
                (stderr.Contains("Traceback") || stderr.Contains("Error:")))
                return $"El script lanzó una excepción: {stderr.Split('\n').LastOrDefault(l => l.Trim().Length > 0) ?? stderr[..Math.Min(200, stderr.Length)]}";

            // Sin stdout → no imprimió nada
            if (string.IsNullOrWhiteSpace(stdout))
                return "El script terminó sin imprimir nada. Debe hacer print(json.dumps(...)) al final.";

            // Intentar parsear como JSON
            string trimmed = stdout.Trim();
            int ini = trimmed.IndexOf('{');
            if (ini >= 0)
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(trimmed[ini..]);
                    // Si tiene status:error → fallo semántico
                    if (doc.RootElement.TryGetProperty("status", out var st) &&
                        st.GetString() == "error")
                    {
                        string det = doc.RootElement.TryGetProperty("detalle", out var d)
                            ? d.GetString() ?? "" : "";
                        return $"El skill reportó error interno: {det}";
                    }
                    return null; // JSON válido con status ok
                }
                catch { }
            }

            // No es JSON pero tiene contenido — aceptar
            if (trimmed.Length >= 10) return null;

            return "Output demasiado corto o sin estructura JSON.";
        }

        /// <summary>
        /// Extrae el bloque .md de la respuesta del LLM.
        /// Maneja correctamente fences anidadas (```python dentro de ```markdown).
        /// </summary>
        private static string ExtraerContenidoMd(string respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta)) return "";
            string r = respuesta.Trim();

            // Caso A: envuelto en ```markdown o ```md
            foreach (var fence in new[] { "```markdown", "```md" })
            {
                if (!r.StartsWith(fence, StringComparison.OrdinalIgnoreCase)) continue;

                int firstNl = r.IndexOf('\n');
                if (firstNl < 0) continue;

                // El cuerpo es todo después de la primera línea de fence
                string body = r[(firstNl + 1)..].TrimEnd();

                // Quitar el cierre ``` final si existe
                if (body.EndsWith("```"))
                    body = body[..^3].TrimEnd();

                if (body.TrimStart().StartsWith("---")) return body.Trim();
            }

            // Caso B: envuelto en ``` genérico — buscar el cierre REAL (último ``` suelto)
            if (r.StartsWith("```"))
            {
                int firstNl = r.IndexOf('\n');
                if (firstNl >= 0)
                {
                    string rest = r[(firstNl + 1)..];

                    // Buscar TODAS las ocurrencias de ``` solos en su línea (cierre real)
                    // El patrón \n```\s*$ no detecta ```python porque tiene texto después
                    var cierres = Regex.Matches(rest, @"\n```[ \t]*(?:\r?\n|$)");
                    if (cierres.Count > 0)
                    {
                        // Tomar el ÚLTIMO cierre para no cortar en bloques ```python internos
                        int endOffset = cierres[cierres.Count - 1].Index;
                        string candidate = rest[..endOffset].Trim();
                        if (candidate.TrimStart().StartsWith("---")) return candidate;
                    }
                    else
                    {
                        // Sin cierre detectado: usar todo el resto
                        string candidate = rest.TrimEnd();
                        if (candidate.EndsWith("```")) candidate = candidate[..^3].TrimEnd();
                        if (candidate.TrimStart().StartsWith("---")) return candidate.Trim();
                    }
                }
            }

            // Caso C: respuesta directa que empieza con frontmatter ---
            if (r.TrimStart().StartsWith("---")) return r;

            return "";
        }

        /// <summary>Extrae código Python limpio de la respuesta del LLM.</summary>
        private static string ExtraerCodigoPython(string respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta)) return "";
            string r = respuesta.Trim();
            foreach (var fence in new[] { "```python", "```py", "```" })
            {
                int ini = r.IndexOf(fence, StringComparison.OrdinalIgnoreCase);
                if (ini < 0) continue;
                int start = r.IndexOf('\n', ini);
                if (start < 0) start = ini + fence.Length;
                else start++;
                int end = r.IndexOf("```", start);
                if (end > start) return r[start..end].Trim();
            }
            if (r.StartsWith("import") || r.StartsWith("from") || r.StartsWith("#"))
                return r;
            return "";
        }

        /// <summary>Construye resumen de credenciales disponibles para el prompt.</summary>
        private string ConstruirDetalleCredenciales()
        {
            if (_listaApis == null || _listaApis.Count == 0)
                return "  (Sin credenciales configuradas)";
            var sb = new StringBuilder();
            foreach (var api in _listaApis)
                sb.AppendLine($"  • {api.Nombre} — {api.Descripcion ?? "sin descripcion"}");
            return sb.ToString();
        }

        /// <summary>Escribe en el log del agente creador (thread-safe).</summary>
        private void LogCreador(string texto, Color color)
        {
            if (InvokeRequired) { Invoke(() => LogCreador(texto, color)); return; }
            _rtbCreadorLog.SelectionStart  = _rtbCreadorLog.TextLength;
            _rtbCreadorLog.SelectionLength = 0;
            _rtbCreadorLog.SelectionColor  = color;
            _rtbCreadorLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {texto}\n");
            _rtbCreadorLog.ScrollToCaret();
        }

        /// <summary>Actualiza el label de estado del agente creador (thread-safe).</summary>
        private void EstadoCreador(string texto, Color color)
        {
            if (InvokeRequired) { Invoke(() => EstadoCreador(texto, color)); return; }
            _lblCreadorEstado.Text      = texto;
            _lblCreadorEstado.ForeColor = color;
        }

        // ── Form closing ──────────────────────────────────────────────────────

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _ctsHub?.Cancel();
            _ctsCreador?.Cancel();
            if (_procesoActual != null && !_procesoActual.HasExited)
            {
                _procesoActual.Kill();
                _procesoActual.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
