using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
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
 
    public partial class FrmRutas : Form
    {

        private List<Archivo> ListaArchivos = new List<Archivo>();

        public FrmRutas()
        {
            InitializeComponent();
            pnlContenedor.FlowDirection = FlowDirection.LeftToRight;
            pnlContenedor.WrapContents = true;
            pnlContenedor.AutoScroll = true;

            // Activar double buffering en el FlowLayoutPanel
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(pnlContenedor, true);
            AplicarThema();


        }

        private void FrmRutas_Load(object sender, EventArgs e)
        {
            CargarDatos();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            if (textBoxRuta.Enabled == true)
            {
                GuardarNueva();
            }
            else
            {
                EditarArchivo();
            }
            CargarDatos();
        }
        private void btnCancelar_Click(object sender, EventArgs e)
        {
            textBoxRuta.Text = "";
            textBoxDescripcion.Text = "";
            textBoxRuta.Enabled = true;
            btnAgregar.Text = "Agregar";
        }

        /// <summary>
        /// Carga y muestra los archivos disponibles en el contenedor visual.
        /// 
        /// Este método:
        /// - Lee la lista de archivos desde el archivo de configuración JSON.
        /// - Limpia los controles existentes del panel contenedor.
        /// - Crea y agrega dinámicamente un panel visual por cada archivo encontrado.
        /// </summary>
        private void CargarDatos()
        {
            ListaArchivos = JsonManager.Leer<Archivo>(RutasProyecto.ObtenerRutaListArchivos());

            NativeMethods.SendMessage(pnlContenedor.Handle, NativeMethods.WM_SETREDRAW, false, 0);
            pnlContenedor.Controls.Clear();

            foreach (var archivo in ListaArchivos)
            {
                pnlContenedor.Controls.Add(CrearPanelArchivo(archivo));
            }

            NativeMethods.SendMessage(pnlContenedor.Handle, NativeMethods.WM_SETREDRAW, true, 0);
            pnlContenedor.Refresh();
        }

        /// <summary>
        /// Guarda un nuevo archivo de trabajo en la configuración del sistema.
        /// 
        /// Este método:
        /// - Obtiene la ruta y descripción ingresadas por el usuario.
        /// - Crea una nueva instancia de <see cref="Archivo"/> con los datos proporcionados.
        /// - Valida que los campos no estén vacíos antes de guardar.
        /// - Agrega el nuevo archivo al archivo de configuración JSON.
        /// - Limpia la lista local de archivos para forzar una recarga posterior.
        /// </summary>
        private void GuardarNueva()
        {
            string ruta = textBoxRuta.Text;
            string desc = textBoxDescripcion.Text;

            Archivo nuevo = new Archivo
            {
                Ruta = ruta,
                Descripcion = desc,
            };

            if (ruta != "" && desc != "")
            {
                JsonManager.Agregar<Archivo>(RutasProyecto.ObtenerRutaListArchivos(), nuevo);
            }

            ListaArchivos.Clear();
        }


      

        /// <summary>
        /// Modifica la descripción de un archivo existente.
        /// 
        /// Este método:
        /// - Obtiene la nueva descripción ingresada por el usuario.
        /// - Valida que la descripción no esté vacía ni contenga solo espacios.
        /// - Actualiza la descripción del archivo correspondiente en el archivo JSON.
        /// - Recarga la lista de archivos para reflejar los cambios en la interfaz.
        /// - Restablece el formulario al modo de agregado.
        /// </summary>
        private void EditarArchivo()
        {
            string nuevaDescripcion = textBoxDescripcion.Text;

            if (!string.IsNullOrWhiteSpace(nuevaDescripcion))
            {

                JsonManager.Modificar<Archivo>(
                    RutasProyecto.ObtenerRutaListArchivos(),
                    a => a.Ruta == textBoxRuta.Text,
                    a => a.Descripcion = nuevaDescripcion
                );

                CargarDatos();
                textBoxRuta.Enabled = true;
                btnAgregar.Text = "Agregar";
            }


        }


        /// <summary>
        /// Elimina un archivo de trabajo tras confirmación del usuario.
        /// 
        /// Este método:
        /// - Muestra un mensaje de confirmación antes de eliminar.
        /// - Elimina el archivo cuya ruta coincide con la proporcionada.
        /// - Actualiza la interfaz recargando la lista de archivos.
        /// </summary>
        /// <param name="ruta">
        /// Ruta del archivo que se desea eliminar.
        /// </param>
        private void EliminarArchivo(string ruta)
        {
            var confirm = MessageBox.Show(
                "¿Desea eliminar este archivo?",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                JsonManager.Eliminar<Archivo>(
                    RutasProyecto.ObtenerRutaListArchivos(),
                    a => a.Ruta == ruta
                );

                CargarDatos();
            }
        }

        private void AbrirRuta(string ruta)
        {
            if (Directory.Exists(ruta))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ruta,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("El archivo no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarEdicion(Archivo archivo)
        {
            textBoxDescripcion.Text = archivo.Descripcion;
            textBoxRuta.Text = archivo.Ruta; ;
            textBoxRuta.Enabled = false;
            btnAgregar.Text = "Modificar";
        }



        /// <summary>
        /// Crea dinámicamente un panel visual que representa un archivo de trabajo.
        /// 
        /// El panel muestra:
        /// - La ruta del archivo.
        /// - La descripción asociada.
        /// - Botones de acción para editar, abrir y eliminar el archivo.
        /// 
        /// El diseño es adaptable al tamaño del contenedor y está pensado para
        /// mostrarse en forma de cuadrícula.
        /// </summary>
        /// <param name="archivo">
        /// Objeto <see cref="Archivo"/> que contiene la información del archivo.
        /// </param>
        /// <returns>
        /// Un <see cref="Panel"/> configurado y listo para agregarse al contenedor.
        /// </returns>
        private Panel CrearPanelArchivo(Archivo archivo)
        {
            // ===== Determinar ícono y color por extensión =====
            string ext = Path.GetExtension(archivo.Ruta)?.ToLower() ?? "";
            (string icono, Color colorAccent) = ext switch
            {
                ".pdf" => ("📄", Color.FromArgb(220, 70, 70)),
                ".txt" => ("📝", Color.FromArgb(80, 150, 200)),
                ".cs" => ("💻", Color.FromArgb(100, 180, 100)),
                ".json" => ("🔧", Color.FromArgb(200, 150, 50)),
                ".xml" => ("🗂", Color.FromArgb(180, 100, 200)),
                ".jpg" or ".png" or
                ".jpeg" or ".gif" => ("🖼", Color.FromArgb(220, 120, 60)),
                ".zip" or ".rar" => ("📦", Color.FromArgb(130, 100, 180)),
                ".xlsx" or ".csv" => ("📊", Color.FromArgb(60, 160, 100)),
                ".docx" or ".doc" => ("📋", Color.FromArgb(50, 100, 200)),
                _ => ("📁", Color.FromArgb(120, 130, 160))
            };

            Color colorFondo = Color.FromArgb(30, 41, 59);
            Color colorFondoBadge = Color.FromArgb(
               30, 41, 59
            );

            // ===== Panel principal =====
            Panel panel = new Panel();
            panel.Width = (pnlContenedor.ClientSize.Width / 3) - 15;
            panel.Height = 160;
            panel.Margin = new Padding(5);
            panel.BackColor = colorFondo;
            panel.Cursor = Cursors.Hand;

            // ===== Barra de acento izquierda (color dinámico) =====
            Panel barraAccent = new Panel();
            barraAccent.Size = new Size(4, panel.Height);
            barraAccent.Location = new Point(0, 0);
            barraAccent.BackColor = colorAccent;

            // ===== Badge ícono por tipo de archivo =====
            Panel badge = new Panel();
            badge.Size = new Size(36, 36);
            badge.Location = new Point(14, 14);
            badge.BackColor = colorFondoBadge;
            badge.RedondearPanel(borderRadius: 8, borderColor: colorFondoBadge);

            Label lblIcono = new Label();
            lblIcono.Text = icono;
            lblIcono.Font = new Font("Segoe UI Emoji", 13, FontStyle.Regular);
            lblIcono.AutoSize = true;
            lblIcono.Location = new Point(5, 5);
            badge.Controls.Add(lblIcono);

            // ===== Chip de extensión =====
            Label lblExt = new Label();
            lblExt.Text = string.IsNullOrEmpty(ext) ? "FILE" : ext.TrimStart('.').ToUpper();
            lblExt.Font = new Font("Segoe UI", 6.5f, FontStyle.Bold);
            lblExt.ForeColor = Color.White;
            lblExt.BackColor = colorAccent;
            lblExt.AutoSize = false;
            lblExt.Size = new Size(34, 14);
            lblExt.TextAlign = ContentAlignment.MiddleCenter;
            lblExt.Location = new Point(14, 52);

            // ===== Nombre del archivo =====
            Label lblNombre = new Label();
            lblNombre.Text = Path.GetFileName(archivo.Ruta);
            lblNombre.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblNombre.ForeColor = Color.White;
            lblNombre.Location = new Point(58, 14);
            lblNombre.Width = panel.Width - 70;
            lblNombre.AutoEllipsis = true;

            // ===== Ruta completa =====
            Label lblRuta = new Label();
            lblRuta.Text = archivo.Ruta;
            lblRuta.Font = new Font("Consolas", 7, FontStyle.Regular);
            lblRuta.ForeColor = Color.FromArgb(150, 160, 190);
            lblRuta.Location = new Point(58, 34);
            lblRuta.Width = panel.Width - 70;
            lblRuta.AutoEllipsis = true;

            // ===== Descripción =====
            Label lblDescripcion = new Label();
            lblDescripcion.Text = archivo.Descripcion;
            lblDescripcion.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblDescripcion.ForeColor = Color.FromArgb(100, 115, 150);
            lblDescripcion.Location = new Point(58, 58);
            lblDescripcion.Width = panel.Width - 70;
            lblDescripcion.Height = 28;
            lblDescripcion.AutoEllipsis = true;

            // ===== Separador =====
            Panel separador = new Panel();
            separador.Size = new Size(panel.Width - 20, 1);
            separador.Location = new Point(10, 100);
            separador.BackColor = Color.FromArgb(215, 220, 240);

            // ===== Botón Editar =====
            Button btnEditar = new Button();
            btnEditar.Text = "✏ Editar";
            btnEditar.Size = new Size(72, 28);
            btnEditar.Location = new Point(14, 112);
            btnEditar.Tag = archivo;
            btnEditar.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            btnEditar.Click += BtnEditarArchivo_Click;
            btnEditar.AplicarEstiloOutline(colorBorde: Color.DarkGreen, borderRadius: 9);

            // ===== Botón Abrir =====
            Button btnAbrir = new Button();
            btnAbrir.Text = "📂 Abrir";
            btnAbrir.Size = new Size(72, 28);
            btnAbrir.Location = new Point(92, 112);
            btnAbrir.Tag = archivo;
            btnAbrir.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            btnAbrir.Click += BtnAbrirArchivo_Click;
            btnAbrir.AplicarEstiloOutline(colorBorde: Color.DarkGray, borderRadius: 9);

            // ===== Botón Eliminar =====
            Button btnEliminar = new Button();
            btnEliminar.Text = "🗑 Borrar";
            btnEliminar.Size = new Size(72, 28);
            btnEliminar.Location = new Point(170, 112);
            btnEliminar.Tag = archivo;
            btnEliminar.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            btnEliminar.Click += BtnEliminarArchivo_Click;
            btnEliminar.AplicarEstiloOutline(colorBorde: Color.DarkRed, borderRadius: 9);

            // ===== Hover effect =====
            panel.MouseEnter += (s, e) => panel.BackColor = Color.FromArgb(30, 48, 59);
            panel.MouseLeave += (s, e) => panel.BackColor = colorFondo;

            // ===== Agregar controles =====
            panel.Controls.Add(barraAccent);
            panel.Controls.Add(badge);
            panel.Controls.Add(lblExt);
            panel.Controls.Add(lblNombre);
            panel.Controls.Add(lblRuta);
            panel.Controls.Add(lblDescripcion);
            panel.Controls.Add(separador);
            panel.Controls.Add(btnEditar);
            panel.Controls.Add(btnAbrir);
            panel.Controls.Add(btnEliminar);

            panel.RedondearPanel(borderRadius: 12, borderColor: Color.FromArgb(210, 215, 240));
            typeof(Panel).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(panel, true);

            return panel;
        }
        private void BtnEditarArchivo_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            Archivo archivo = btn.Tag as Archivo;

            ConfigurarEdicion(archivo);

        }

        private void BtnAbrirArchivo_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            Archivo archivo = btn.Tag as Archivo;

            AbrirRuta(archivo.Ruta);
        }

        private void BtnEliminarArchivo_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            Archivo archivo = btn.Tag as Archivo;

            EliminarArchivo(archivo.Ruta);
        }

        private void AplicarThema()
        {
            textBoxRuta.RedondearTextBox(borderRadius: 10, borderColor: Color.RoyalBlue);
            textBoxDescripcion.RedondearTextBox(borderRadius: 10, borderColor: Color.RoyalBlue);
            btnAgregar.AplicarEstiloOutline(
                colorBorde: Color.RoyalBlue,
                borderRadius: 9
            );
            btnCancelar.AplicarEstiloOutline(
                colorBorde: Color.DarkRed,
                borderRadius: 9
            );
            panelFormulario.Redondear();

        }

    }
}
