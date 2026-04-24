using Microsoft.Extensions.DependencyInjection;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmPrincipal : Form
    {

        private ConfiguracionClient Miconfiguracion = new ConfiguracionClient();
        private readonly AutomatizacionScheduler _scheduler = new();
        private readonly IServiceProvider _services;

        public FrmPrincipal(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
            AplicarTema();
            CargarDatosInicio();
            btnComunicadores.Visible = true;
            btnComunicadores.BringToFront(); // Garantiza z-order al frente tras el fix de Y=408.
           // AgregarBotonesAgentesAvanzados();
            // pnlMenu se queda corto con los botones nuevos del Fase A/B/C.
            // Habilitamos scroll vertical por si la ventana es chica — así
            // los botones de abajo siguen alcanzables.
            pnlMenu.AutoScroll = true;

            AgregarBotonHabilidades();  // slot único (0, 267)
            AgregarBotonMemoria();      // relocado a (0, 595)
            AgregarBotonPatrones();     // relocado a (0, 640)
            AgregarBotonTokens();       // relocado a (0, 685)
            AgregarBotonEmbeddings();   // relocado a (0, 730)
            AgregarBotonTraces();       // Fase 1A → (0, 775)
        }

        /// <summary>
        /// Añade el botón "🔬 Traces" — Fase 1A del sistema de observabilidad.
        /// Abre el panel flotante (TopMost-style) de traces: lista de
        /// ejecuciones del día, árbol de spans y detalles de cada span.
        /// Al igual que Tokens, es flotante — así el usuario puede observar
        /// un pipeline en vivo mientras trabaja en cualquier otra pantalla.
        /// </summary>
        private void AgregarBotonTraces()
        {
            var btnTraces = CrearBotonMenu("🔬  Traces", new Point(0, 775));
            btnTraces.Click += btnTraces_Click;
            pnlMenu.Controls.Add(btnTraces);
            btnTraces.BringToFront();
        }

        private void btnTraces_Click(object sender, EventArgs e)
        {
            FrmTraces.MostrarOTraerAlFrente(this);
        }

        /// <summary>
        /// Añade el botón "🧬 Embeddings" — Fase C del sistema de tokens.
        /// Configura proveedor (Ollama/OpenAI), parámetros de RAG e
        /// ejecuta/limpia la indexación semántica de la memoria.
        /// </summary>
        private void AgregarBotonEmbeddings()
        {
            // Y=502 chocaba con btnRutas del Designer → relocado.
            var btnEmb = CrearBotonMenu("🧬  Embeddings", new Point(0, 730));
            btnEmb.Click += btnEmbeddings_Click;
            pnlMenu.Controls.Add(btnEmb);
            btnEmb.BringToFront();
        }

        private void btnEmbeddings_Click(object sender, EventArgs e)
        {
            string ruta = Miconfiguracion?.MiArchivo?.Ruta ?? "";
            FrmEmbeddings frm = new FrmEmbeddings(ruta);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frm);
        }

        /// <summary>
        /// Añade el botón "📊 Tokens" que abre el panel flotante de consumo
        /// de tokens por LLM. A diferencia de los demás, este NO se abre dentro
        /// de pnlContenedor — es un form flotante (TopMost) para que el usuario
        /// pueda ver el consumo mientras trabaja en cualquier otra pantalla.
        /// </summary>
        private void AgregarBotonTokens()
        {
            // Y=455 chocaba con btnApis del Designer → relocado a zona libre
            // bajo button1 (Y=549). BringToFront garantiza z-order correcto
            // (Controls.Add append al FINAL → detrás; BringToFront → al frente).
            var btnTokens = CrearBotonMenu("📊  Tokens", new Point(0, 685));
            btnTokens.Click += btnTokens_Click;
            pnlMenu.Controls.Add(btnTokens);
            btnTokens.BringToFront();
        }

        private void btnTokens_Click(object sender, EventArgs e)
        {
            FrmConsumoTokens.MostrarOTraerAlFrente(this);
        }

        /// <summary>
        /// Añade el botón "🧠 Memoria" al menú lateral sin modificar el Designer.
        /// La memoria vive dentro de la ruta de trabajo activa
        /// (Miconfiguracion.MiArchivo.Ruta), así que se pasa al abrir el form.
        /// </summary>
        private void AgregarBotonMemoria()
        {
            // Y=361 chocaba con btnAutomatizacion del Designer → relocado.
            var btnMemoria = CrearBotonMenu("🧠  Memoria", new Point(0, 595));
            btnMemoria.Click += btnMemoria_Click;
            pnlMenu.Controls.Add(btnMemoria);
            btnMemoria.BringToFront();
        }

        private void btnMemoria_Click(object sender, EventArgs e)
        {
            string ruta = Miconfiguracion?.MiArchivo?.Ruta ?? "";
            FrmMemoria frm = new FrmMemoria(ruta);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frm);
        }

        /// <summary>
        /// Añade el botón "⚙ Habilidades" — toggles para activar/desactivar
        /// capacidades cognitivas del agente (memoria, patrones, sugerencias...).
        /// Distinto de Skills.cs (plugins Python externos).
        /// </summary>
        private void AgregarBotonHabilidades()
        {
            // (0, 267) es el único slot del Designer sin botón → sin conflicto.
            var btnHabilidades = CrearBotonMenu("⚙  Habilidades", new Point(0, 267));
            btnHabilidades.Click += btnHabilidades_Click;
            pnlMenu.Controls.Add(btnHabilidades);
            btnHabilidades.BringToFront();
        }

        private void btnHabilidades_Click(object sender, EventArgs e)
        {
            FrmHabilidades frm = new FrmHabilidades();
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frm);
        }

        /// <summary>
        /// Añade el botón "🔎 Patrones" — Fase 3 del sistema de Memoria.
        /// Detecta tareas recurrentes en Episodios.md y propone convertirlas
        /// en Skills. Solo dispara el análisis cuando el usuario lo pide —
        /// nunca en cada pipeline — para que el coste de tokens sea visible.
        /// </summary>
        private void AgregarBotonPatrones()
        {
            // Y=408 chocaba con btnPromts/btnComunicadores del Designer → relocado.
            var btnPatrones = CrearBotonMenu("🔎  Patrones", new Point(0, 640));
            btnPatrones.Click += btnPatrones_Click;
            pnlMenu.Controls.Add(btnPatrones);
            btnPatrones.BringToFront();
        }

        private void btnPatrones_Click(object sender, EventArgs e)
        {
            string ruta = Miconfiguracion?.MiArchivo?.Ruta ?? "";
            FrmPatrones frm = new FrmPatrones(ruta, Miconfiguracion);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frm);
        }

        private void btnMando_Click(object sender, EventArgs e)
        {
            // ActivatorUtilities mezcla los servicios registrados en el container
            // con el parámetro runtime (Miconfiguracion) que varía por sesión.
            var frmMandos = ActivatorUtilities.CreateInstance<FrmMandos>(_services, Miconfiguracion);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmMandos);
        }

        private void btnApis_Click(object sender, EventArgs e)
        {
            FrmApis frmApis = new FrmApis();
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmApis);
        }

        private void btnRutas_Click(object sender, EventArgs e)
        {
            FrmRutas frmRutas = new FrmRutas();
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmRutas);
        }

        private void btnModelos_Click(object sender, EventArgs e)
        {
            FrmModelos frmModelos = new FrmModelos();
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmModelos);
        }

        private void btnMultiples_Click(object sender, EventArgs e)
        {
            var frmAgentes = ActivatorUtilities.CreateInstance<FrmMultopleAngent>(_services);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmAgentes);
        }
        private void btnAutomatizacion_Click(object sender, EventArgs e)
        {
            FrmAutomatizaciones frmAutomatizaciones = new FrmAutomatizaciones(Miconfiguracion);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmAutomatizaciones);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            EmeraldTheme.CloseCurrentForm(pnlContenedor);

            foreach (Control control in pnlContenedor.Controls)
            {
                if (control is Form form)
                {
                    form.Show();
                    form.BringToFront();
                    pnlContenedor.Tag = form;
                }
            }


        }

        private void btnSalir_Click(object sender, EventArgs e)
        {
            _scheduler.Dispose();
            this.Close();
        }
        private void btnSkills_Click(object sender, EventArgs e)
        {
            Skills fmrSkills = new Skills(Miconfiguracion);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, fmrSkills);
        }

        private void btnComunicadores_Click(object sender, EventArgs e)
        {
            FrmComunicadores frmComunicadores = new FrmComunicadores(Miconfiguracion);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmComunicadores);
        }

        private void btnPromts_Click(object sender, EventArgs e)
        {
            FrmPromts frmPromts = new FrmPromts();
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frmPromts);
        }


        private void CargarDatosInicio()
        {
            Miconfiguracion = Utils.LeerConfig<ConfiguracionClient>(RutasProyecto.ObtenerRutaConfiguracion());
            IniciarScheduler();
        }

        private void IniciarScheduler()
        {
            try
            {
                var apis = JsonManager.Leer<Api>(RutasProyecto.ObtenerRutaListApis());
                string claves = Utils.ObtenerNombresApis(apis);

                _scheduler.Configurar(
                    Miconfiguracion?.Mimodelo?.Modelos ?? "",
                    Miconfiguracion?.Mimodelo?.ApiKey  ?? "",
                    Miconfiguracion?.MiArchivo?.Ruta   ?? RutasProyecto.ObtenerRutaScripts(),
                    claves,
                    Miconfiguracion?.Mimodelo?.Agente  ?? Servicios.Gemenni);

                _scheduler.Iniciar();
            }
            catch { }
        }

        private void AplicarTema()
        {

        }

        // ── Agentes Avanzados (1.3 y 1.4) ───────────────────────────────────

        /// <summary>
        /// Agrega los botones de Agentes Avanzados al menú lateral sin modificar
        /// el Designer. Se insertan entre btnModelos (Y=158) y la sección
        /// CONFIGURACION (label2, Y=296).
        /// </summary>
        private void AgregarBotonesAgentesAvanzados()
        {
            var btnPlanificacion = CrearBotonMenu("🧠  Agente Planificador", new Point(0, 200));
            btnPlanificacion.Click += (s, e) => AbrirAgentesAvanzados(modoPipeline: false);
            pnlMenu.Controls.Add(btnPlanificacion);

            var btnPipeline = CrearBotonMenu("🔗  Pipeline Multi-Agente", new Point(0, 248));
            btnPipeline.Click += (s, e) => AbrirAgentesAvanzados(modoPipeline: true);
            pnlMenu.Controls.Add(btnPipeline);
        }

        private void AbrirAgentesAvanzados(bool modoPipeline)
        {
            var frm = new FrmPipelineAgente();
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, frm);
        }

        private Button CrearBotonMenu(string texto, Point ubicacion)
        {
            var btn = new Button
            {
                Text = texto,
                Location = ubicacion,
                Size = new Size(203, 41),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0),
                ForeColor = Color.FromArgb(148, 163, 184),
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = true,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 0, 64);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(192, 192, 255);
            return btn;
        }

        private void btnOcultar_Click(object sender, EventArgs e)
        {

            if (pnlMenu.Width == 90)
            {
                pnlMenu.Size = new Size(203, 744);
                CambiarSize_btn(203);
            }
            else
            {
                pnlMenu.Size = new Size(90, 622);
                CambiarSize_btn(90);
            }

        }


        private void CambiarSize_btn(int tam)
        {
            foreach (var item in pnlMenu.Controls)
            {
                if (item is Button Mibuton)
                {
                    Mibuton.Width = tam;
                }
            }
            btnSalir.Width = tam;
        }

       
    }
}
