using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmMultopleAngent : Form
    {
        private readonly IServiceProvider _services;

        public FrmMultopleAngent(IServiceProvider services)
        {
            _services = services;
            InitializeComponent();
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanel1.WrapContents = false;

        }


        private void ObetnerCantidadFrm(int N)
        {
            CrearFormularios(N);
        }

        private void CrearFormularios(int N)
        {
            for (int i = 0; i < N; i++)
            {
                var frm = _services.GetRequiredService<FrmPrincipal>();
                AgregarFormulario(frm);
            }
        }


        private void AgregarFormulario(Form frm)
        {
            frm.TopLevel = false;
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Dock = DockStyle.None;
            frm.Width = flowLayoutPanel1.Width - 20;

            // Contenedor
            Panel contenedor = new Panel();
            contenedor.Width = flowLayoutPanel1.Width - 10;
            contenedor.Height = frm.Height + 2;
            contenedor.Margin = new Padding(0);
            contenedor.AutoScroll = true;
            // Línea divisora
            Panel linea = new Panel();
            linea.Height = 1;
            linea.Dock = DockStyle.Bottom;
            linea.BackColor = Color.LightGray;

            contenedor.Controls.Add(frm);
            contenedor.Controls.Add(linea);

            flowLayoutPanel1.Controls.Add(contenedor);
            frm.Show();
        }

        private void btnCrear_Click(object sender, EventArgs e)
        {
            int cantidad = (int)numeriCantidad.Value;
            ObetnerCantidadFrm(cantidad);

        }
    }
}
