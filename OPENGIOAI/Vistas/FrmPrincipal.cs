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

        public FrmPrincipal()
        {
            InitializeComponent();
            AplicarTema();
            CargarDatosInicio();
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
            FrmAutomatizaciones frmAutomatizaciones = new FrmAutomatizaciones();
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
        }
        private void AplicarTema()
        {
      
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
