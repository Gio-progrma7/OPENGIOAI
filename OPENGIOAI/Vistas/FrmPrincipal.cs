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

        private string RutaTrabajo = "";
        private ConfiguracionClient Miconfiguracion = new ConfiguracionClient();
        private readonly AutomatizacionScheduler _scheduler = new();

        public FrmPrincipal()
        {
            InitializeComponent();
            AplicarTema();
            CargarDatosInicio();
           // AgregarBotonesAgentesAvanzados();
        }

        private void btnMando_Click(object sender, EventArgs e)
        {
            FrmMandos frmMandos = new FrmMandos(Miconfiguracion);
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
            FrmMultopleAngent frmAgentes = new FrmMultopleAngent();
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
            Skills fmrSkills = new Skills(Miconfiguracion.MiArchivo.Ruta);
            EmeraldTheme.OpenOrShowFormInPanel(pnlContenedor, fmrSkills);
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
