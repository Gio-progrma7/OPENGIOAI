using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmApis : Form
    {

        private List<Api> MisApis = new List<Api>();
        private bool EsNueva = true;
        private string ApiSeleccionada = "";


        public FrmApis()
        {
            InitializeComponent();
            pnlApis.FlowDirection = FlowDirection.LeftToRight;
            pnlApis.WrapContents = true;
            pnlApis.AutoScroll = true;

            Aplicar_thema();
            pnlApis.MouseWheel += pnlApis_MouseWheel;
            pnlApis.Focus(); // importante para que detecte el scroll

        }

        private void FrmApis_Load(object sender, EventArgs e)
        {
            CargarApis();
        }

        private void btnGuadar_Click(object sender, EventArgs e)
        {
            if (EsNueva)
            {
                GuardarNueva();
            }
            else
            {
                Modificar();
            }

        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            CancelarEdicion();
        }

        private void textBoxFiltro_TextChanged(object sender, EventArgs e)
        {
            FiltrarApis(textBoxFiltro.Text);
        }

        private void pnlApis_MouseWheel(object sender, MouseEventArgs e)
        {
            Panel panel = sender as Panel;

            // Ajusta este valor (más pequeño = menos sensibilidad)
            int scrollAmount = 10;

            int newValue = panel.VerticalScroll.Value - (e.Delta / 120) * scrollAmount;

            // Limitar rango
            newValue = Math.Max(panel.VerticalScroll.Minimum, newValue);
            newValue = Math.Min(panel.VerticalScroll.Maximum, newValue);

            panel.VerticalScroll.Value = newValue;
            panel.PerformLayout();
        }
        /// <summary>
        /// Carga y muestra dinámicamente las APIs disponibles en el panel de configuración.
        /// 
        /// Este método:
        /// - Limpia los controles existentes del panel de APIs.
        /// - Obtiene la ruta del archivo que contiene la lista de APIs.
        /// - Lee y deserializa las APIs desde el archivo JSON.
        /// - Crea y agrega un panel visual por cada API encontrada.
        /// </summary>
        private void CargarApis()
        {
            // Suspender layout evita múltiples repintados mientras se agregan controles
            pnlApis.SuspendLayout();
            pnlApis.Controls.Clear();

            string ruta = RutasProyecto.ObtenerRutaListApis();
            MisApis = JsonManager.Leer<Api>(ruta);

            var paneles = MisApis.Select(api => CrearPanelApi(api)).ToArray();
            pnlApis.Controls.AddRange(paneles); // AddRange es más eficiente que Add en loop

            pnlApis.ResumeLayout(true);
        }


        // 3. Reemplazar FiltrarApis() igualmente:
        private void FiltrarApis(string filtro)
        {
            pnlApis.SuspendLayout();
            pnlApis.Controls.Clear();

            var apisFiltradas = string.IsNullOrWhiteSpace(filtro)
                ? MisApis
                : MisApis.Where(a => a.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();

            var paneles = apisFiltradas.Select(api => CrearPanelApi(api)).ToArray();
            pnlApis.Controls.AddRange(paneles);

            pnlApis.ResumeLayout(true);
        }


        /// <summary>
        /// Guarda una nueva API ingresada por el usuario en el sistema.
        /// 
        /// Este método:
        /// - Valida que los campos de nombre y clave no estén vacíos.
        /// - Obtiene los valores ingresados por el usuario.
        /// - Agrega la nueva API al archivo de configuración en formato JSON.
        /// - Recarga la lista visual de APIs para reflejar los cambios.
        /// - Limpia los campos de entrada después de guardar.
        /// </summary>
        private void GuardarNueva()
        {

            if (textBoxNombre.Text == "" && textBoxKey.Text == "")
            {
                return;
            }

            string nombre = textBoxNombre.Text;
            string key = textBoxKey.Text;
            string descripcion = textBoxDescripcion.Text;

            JsonManager.Agregar(RutasProyecto.ObtenerRutaListApis(), new Api
            {
                Nombre = nombre,
                key = key,
                Descripcion = descripcion
            });

            CargarApis();
            LimpiarCampos();
        }


        /// <summary>
        /// Modifica los datos de una API existente.
        /// 
        /// Este método:
        /// - Valida que los campos de nombre y clave no estén vacíos.
        /// - Obtiene los nuevos valores ingresados por el usuario.
        /// - Actualiza la API seleccionada en el archivo de configuración JSON.
        /// - Restablece el estado del formulario a modo "Agregar".
        /// - Recarga la lista de APIs para reflejar los cambios.
        /// - Limpia los campos de entrada después de la modificación.
        /// </summary>
        private void Modificar()
        {
            if (textBoxNombre.Text == "" && textBoxKey.Text == "" && textBoxDescripcion.Text == "")
            {
                return;
            }

            string nombre = textBoxNombre.Text;
            string key = textBoxKey.Text;
            string descripcion = textBoxDescripcion.Text;

            JsonManager.Modificar<Api>(
                 RutasProyecto.ObtenerRutaListApis(),
                 u => u.Nombre == ApiSeleccionada, // condición
                 u =>
                 {
                     u.key = key;
                     u.Nombre = nombre;
                     u.Descripcion = descripcion;
                 }
             );

            btnGuadar.Text = "Agregar";
            EsNueva = true;
            CargarApis();
            LimpiarCampos();
        }

        /// <summary>
        /// Elimina una API existente del sistema.
        /// 
        /// Este método:
        /// - Busca la API por su nombre dentro del archivo de configuración.
        /// - Elimina la API que coincide con el nombre proporcionado.
        /// - Recarga la lista visual de APIs para reflejar los cambios.
        /// </summary>
        /// <param name="nombre">
        /// Nombre de la API que se desea eliminar.
        /// </param>
        private void Eliminar(string nombre)
        {
            JsonManager.Eliminar<Api>(RutasProyecto.ObtenerRutaListApis(),
                u => u.Nombre == nombre
            );
            CargarApis();
        }


        #region Creación dinámica de paneles para APIs

        /// <summary>
        /// Crea dinámicamente un panel visual que representa una API registrada.
        /// 
        /// El panel incluye:
        /// - El nombre de la API.
        /// - La clave asociada (mostrada de forma responsiva y truncada si es necesario).
        /// - Un botón para editar la API.
        /// - Un botón para eliminar la API.
        /// 
        /// El diseño es adaptable al ancho del contenedor y utiliza estilos visuales
        /// modernos con bordes redondeados.
        /// </summary>
        /// <param name="api">
        /// Objeto <see cref="Api"/> que contiene la información de la API a mostrar.
        /// </param>
        /// <returns>
        /// Un <see cref="Panel"/> completamente configurado y listo para agregarse a la interfaz.
        /// </returns>
        private Panel CrearPanelApi(Api api)
        {

            PanelSuave panel = new PanelSuave();

            // Y el panel del badge también:
            PanelSuave badge = new PanelSuave();
            PanelSuave barraAccent = new PanelSuave();
            PanelSuave separador = new PanelSuave();

            

            // 5. Hover con BackColor directo (sin lambda extra), más liviano:
            // Cambiar los eventos de hover a esto:


            // ===== Panel principal =====
            panel.Width = (pnlApis.DisplayRectangle.Width / 2) - 15;
            panel.Height = 155;
            panel.Margin = new Padding(8);
            panel.BackColor = Color.FromArgb(30, 41, 59);
            panel.Cursor = Cursors.Hand;

            // ===== Barra de acento izquierda =====
            barraAccent.Size = new Size(4, panel.Height);
            barraAccent.Location = new Point(0, 0);
            barraAccent.BackColor = Color.RoyalBlue;

            // ===== Ícono / Badge de estado =====
            badge.Size = new Size(36, 36);
            badge.Location = new Point(18, 14);
            badge.BackColor = Color.FromArgb(30, 41, 59);
            badge.RedondearPanel(borderRadius: 8, borderColor: Color.FromArgb(220, 230, 255));

            Label lblIcono = new Label();
            lblIcono.Text = "⚡";
            lblIcono.Font = new Font("Segoe UI Emoji", 14, FontStyle.Regular);
            lblIcono.ForeColor = Color.RoyalBlue;
            lblIcono.AutoSize = true;
            lblIcono.Location = new Point(6, 6);
            badge.Controls.Add(lblIcono);

            // ===== Nombre =====
            Label lblNombre = new Label();
            lblNombre.Text = api.Nombre;
            lblNombre.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            lblNombre.ForeColor = Color.White;
            lblNombre.Location = new Point(62, 14);
            lblNombre.Width = panel.Width - 75;
            lblNombre.AutoEllipsis = true;

            // ===== Key con estilo chip =====
            Label lblKeyPrefix = new Label();
            lblKeyPrefix.Text = "KEY";
            lblKeyPrefix.Font = new Font("Segoe UI", 7, FontStyle.Bold);
            lblKeyPrefix.ForeColor = Color.White;
            lblKeyPrefix.BackColor = Color.RoyalBlue;
            lblKeyPrefix.AutoSize = false;
            lblKeyPrefix.Size = new Size(32, 16);
            lblKeyPrefix.TextAlign = ContentAlignment.MiddleCenter;
            lblKeyPrefix.Location = new Point(62, 40);

            Label lblKey = new Label();
            lblKey.Text = api.key;
            lblKey.Font = new Font("Consolas", 8, FontStyle.Regular);
            lblKey.ForeColor = Color.FromArgb(80, 100, 140);
            lblKey.Location = new Point(98, 40);
            lblKey.Width = panel.Width - 110;
            lblKey.AutoEllipsis = true;

            // ===== Descripción =====
            Label lblDescripcion = new Label();
            lblDescripcion.Text = api.Descripcion;
            lblDescripcion.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblDescripcion.ForeColor = Color.FromArgb(110, 120, 150);
            lblDescripcion.Location = new Point(62, 62);
            lblDescripcion.Width = panel.Width - 75;
            lblDescripcion.Height = 30;
            lblDescripcion.AutoEllipsis = true;

            // ===== Separador =====
            separador.Size = new Size(panel.Width - 20, 1);
            separador.Location = new Point(10, 100);
            separador.BackColor = Color.FromArgb(220, 225, 240);

            // ===== Botón Editar =====
            Button btnEditar = new Button();
            btnEditar.Text = "✏ Editar";
            btnEditar.Size = new Size(80, 30);
            btnEditar.Location = new Point(14, 112);
            btnEditar.Tag = api;
            btnEditar.Click += BtnEditar_Click;
            btnEditar.AplicarEstiloOutline(Color.DarkGreen, 9);
            btnEditar.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnEditar.FlatAppearance.BorderSize = 1;

            // ===== Botón Eliminar =====
            Button btnEliminar = new Button();
            btnEliminar.Text = "🗑 Eliminar";
            btnEliminar.Size = new Size(82, 30);
            btnEliminar.Location = new Point(102, 112);
            btnEliminar.Tag = api;
            btnEliminar.Click += BtnEliminar_Click;
            btnEliminar.AplicarEstiloOutline(Color.DarkRed, 9);
            btnEliminar.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            btnEliminar.FlatAppearance.BorderSize = 1;

            // ===== Hover effect =====
            panel.MouseEnter += (s, e) => ((PanelSuave)s).BackColor = Color.FromArgb(30, 48, 59);
            panel.MouseLeave += (s, e) => ((PanelSuave)s).BackColor = Color.FromArgb(30, 41, 59);


            // ===== Agregar controles =====
            panel.Controls.Add(barraAccent);
            panel.Controls.Add(badge);
            panel.Controls.Add(lblNombre);
            panel.Controls.Add(lblKeyPrefix);
            panel.Controls.Add(lblKey);
            panel.Controls.Add(lblDescripcion);
            panel.Controls.Add(separador);
            panel.Controls.Add(btnEditar);
            panel.Controls.Add(btnEliminar);

            panel.RedondearPanel(borderRadius: 12, borderColor: Color.FromArgb(200, 210, 240));
            pnlControl.Redondear();
            return panel;
        }



        private void BtnEditar_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            Api api = btn.Tag as Api;

            textBoxNombre.Text = api.Nombre;
            textBoxKey.Text = api.key;
            textBoxDescripcion.Text = api.Descripcion;

            ApiSeleccionada = api.Nombre;
            btnGuadar.Text = "Modificar";
            EsNueva = false;
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            Api api = btn.Tag as Api;

            Eliminar(api.Nombre);

        }

        #endregion  

        private void LimpiarCampos()
        {
            textBoxNombre.Text = "";
            textBoxKey.Text = "";
            textBoxDescripcion.Text = "";
        }

        private void Aplicar_thema()
        {
            Color inputBorder = Color.FromArgb(51, 65, 85);
            textBoxNombre.RedondearTextBox(borderRadius: 10, borderColor: inputBorder);
            textBoxKey.RedondearTextBox(borderRadius: 10, borderColor: inputBorder);
            textBoxDescripcion.RedondearTextBox(borderRadius: 10, borderColor: inputBorder);
            textBoxFiltro.RedondearTextBox(borderRadius: 10, borderColor: Color.DarkGreen);

            btnGuadar.AplicarEstiloOutline(
                colorBorde: Color.RoyalBlue,
                borderRadius: 9
            );
            btnCancelar.AplicarEstiloOutline(
                colorBorde: Color.DarkRed,
                borderRadius: 9
            );


        }

        private void CancelarEdicion()
        {
            LimpiarCampos();
            btnGuadar.Text = "Guardar";
        }

       
    }
}
