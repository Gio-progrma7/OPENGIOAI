using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Clase con métodos para redondear Paneles y aplicar estilos modernos
    /// </summary>
    public static class PanelRounder
    {
        #region Método Principal

        /// <summary>
        /// Redondea un Panel con diseño moderno y profesional
        /// </summary>
        /// <param name="panel">Panel a redondear</param>
        /// <param name="borderRadius">Radio de las esquinas (default: 15)</param>
        /// <param name="borderColor">Color del borde (default: gris claro)</param>
        /// <param name="borderSize">Grosor del borde (default: 0 = sin borde)</param>
        /// <param name="agregarSombra">Agregar sombra profesional (default: true)</param>
        public static void RedondearPanel(
            this Panel panel,
            int borderRadius = 15,
            Color? borderColor = null,
            int borderSize = 0,
            bool agregarSombra = false) // Sombra desactivada por defecto
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            // Configurar el panel para doble buffering
            var prop = typeof(Control).GetProperty("DoubleBuffered", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(panel, true, null);

            // Redondeo simple usando Region
            panel.Resize += (s, e) =>
            {
                if (panel.Width > 0 && panel.Height > 0)
                {
                    using (GraphicsPath path = ObtenerRectanguloRedondeado(panel.ClientRectangle, borderRadius))
                    {
                        panel.Region = new Region(path);
                    }
                }
            };

            if (panel.Width > 0 && panel.Height > 0)
            {
                using (GraphicsPath path = ObtenerRectanguloRedondeado(panel.ClientRectangle, borderRadius))
                {
                    panel.Region = new Region(path);
                }
            }

            // Borde simple con propiedades nativas no es posible para redondeados,
            // pero mantenemos el Paint solo si es estrictamente necesario y ligero.
            if (borderSize > 0)
            {
                Color colorBorde = borderColor ?? Color.FromArgb(200, 200, 200);
                panel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = ObtenerRectanguloRedondeado(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), borderRadius))
                    using (Pen pen = new Pen(colorBorde, borderSize))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                };
            }
        }

        #endregion

        #region Sobrecarga Simplificada

        /// <summary>
        /// Redondea un Panel con valores predeterminados
        /// </summary>
        public static void Redondear(this Panel panel)
        {
            RedondearPanel(panel);
        }

        #endregion

        #region Variantes con Estilos Predefinidos

        /// <summary>
        /// Aplica estilo Card moderno (blanco con sombra)
        /// </summary>
        public static void AplicarEstiloCard(this Panel panel, int borderRadius = 15)
        {
            panel.BackColor = Color.White;
            RedondearPanel(
                panel,
                borderRadius,
                Color.FromArgb(230, 230, 230),
                0, // Sin borde visible
                true // Con sombra
            );
        }

        /// <summary>
        /// Aplica estilo Panel con borde Primary (azul)
        /// </summary>
        public static void AplicarEstiloPrimary(this Panel panel, int borderRadius = 12)
        {
            RedondearPanel(
                panel,
                borderRadius,
                Color.FromArgb(64, 158, 255),
                2,
                true
            );
        }

        /// <summary>
        /// Aplica estilo Success (verde)
        /// </summary>
        public static void AplicarEstiloSuccess(this Panel panel, int borderRadius = 12)
        {
            panel.BackColor = Color.FromArgb(240, 255, 244);
            RedondearPanel(
                panel,
                borderRadius,
                Color.FromArgb(46, 204, 113),
                2,
                false
            );
        }

        /// <summary>
        /// Aplica estilo Warning (amarillo/naranja)
        /// </summary>
        public static void AplicarEstiloWarning(this Panel panel, int borderRadius = 12)
        {
            panel.BackColor = Color.FromArgb(255, 249, 230);
            RedondearPanel(
                panel,
                borderRadius,
                Color.FromArgb(255, 193, 7),
                2,
                false
            );
        }

        /// <summary>
        /// Aplica estilo Danger (rojo)
        /// </summary>
        public static void AplicarEstiloDanger(this Panel panel, int borderRadius = 12)
        {
            panel.BackColor = Color.FromArgb(255, 240, 240);
            RedondearPanel(
                panel,
                borderRadius,
                Color.FromArgb(231, 76, 60),
                2,
                false
            );
        }

        /// <summary>
        /// Aplica estilo oscuro (Dark Theme)
        /// </summary>
        public static void AplicarEstiloOscuro(this Panel panel, int borderRadius = 15)
        {
            panel.BackColor = Color.FromArgb(45, 45, 48);
            panel.ForeColor = Color.White;
            RedondearPanel(
                panel,
                borderRadius,
                Color.FromArgb(70, 70, 73),
                1,
                true
            );
        }

        /// <summary>
        /// Aplica estilo de contenedor con gradiente
        /// </summary>
        public static void AplicarEstiloGradiente(
            this Panel panel, 
            Color colorInicio, 
            Color colorFin, 
            int borderRadius = 15)
        {
            panel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = panel.ClientRectangle;
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    rect, colorInicio, colorFin, LinearGradientMode.Vertical))
                using (GraphicsPath path = ObtenerRectanguloRedondeado(rect, borderRadius))
                {
                    g.FillPath(brush, path);
                }
            };

            RedondearPanel(panel, borderRadius, Color.Transparent, 0, true);
        }

        #endregion

        #region Métodos Helper Privados

        /// <summary>
        /// Genera un GraphicsPath con bordes redondeados de forma profesional
        /// </summary>
        private static GraphicsPath ObtenerRectanguloRedondeado(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();

            // Asegurar que el radio no sea mayor que la mitad del lado más pequeño
            int radioMaximo = Math.Min(rect.Width, rect.Height) / 2;
            radio = Math.Min(radio, radioMaximo);

            float diametro = radio * 2F;

            // Crear path con curvas suaves
            path.StartFigure();

            // Esquina superior izquierda
            path.AddArc(rect.X, rect.Y, diametro, diametro, 180, 90);

            // Esquina superior derecha
            path.AddArc(rect.Right - diametro, rect.Y, diametro, diametro, 270, 90);

            // Esquina inferior derecha
            path.AddArc(rect.Right - diametro, rect.Bottom - diametro, diametro, diametro, 0, 90);

            // Esquina inferior izquierda
            path.AddArc(rect.X, rect.Bottom - diametro, diametro, diametro, 90, 90);

            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// Dibuja una sombra profesional (Eliminado por optimización)
        /// </summary>
        private static void DibujarSombraProfesional(Graphics g, GraphicsPath path, Rectangle bounds)
        {
            // Eliminado para maximizar fluidez
        }

        #endregion

        #region Métodos para Múltiples Paneles

        /// <summary>
        /// Redondea múltiples Paneles a la vez
        /// </summary>
        public static void RedondearMultiples(int borderRadius, params Panel[] panels)
        {
            if (panels == null || panels.Length == 0)
                return;

            foreach (var panel in panels)
            {
                if (panel != null)
                {
                    RedondearPanel(panel, borderRadius);
                }
            }
        }

        /// <summary>
        /// Redondea todos los Paneles de un formulario
        /// </summary>
        public static void RedondearTodosEnFormulario(Form formulario, int borderRadius = 15)
        {
            if (formulario == null)
                throw new ArgumentNullException(nameof(formulario));

            foreach (Control control in formulario.Controls)
            {
                if (control is Panel panel)
                {
                    RedondearPanel(panel, borderRadius);
                }
                else if (control.HasChildren)
                {
                    RedondearTodosEnContenedor(control, borderRadius);
                }
            }
        }

        /// <summary>
        /// Redondea todos los Paneles dentro de un contenedor
        /// </summary>
        private static void RedondearTodosEnContenedor(Control contenedor, int borderRadius)
        {
            foreach (Control control in contenedor.Controls)
            {
                if (control is Panel panel)
                {
                    RedondearPanel(panel, borderRadius);
                }
                else if (control.HasChildren)
                {
                    RedondearTodosEnContenedor(control, borderRadius);
                }
            }
        }

        #endregion

        #region Métodos Avanzados

        /// <summary>
        /// Aplica efecto hover al panel
        /// </summary>
        public static void AgregarEfectoHover(
            this Panel panel, 
            Color colorNormal, 
            Color colorHover)
        {
            Color colorOriginal = panel.BackColor;

            panel.MouseEnter += (s, e) =>
            {
                panel.BackColor = colorHover;
            };

            panel.MouseLeave += (s, e) =>
            {
                panel.BackColor = colorNormal;
            };
        }

        /// <summary>
        /// Aplica efecto de elevación (Simplificado por optimización)
        /// </summary>
        public static void AplicarElevacion(this Panel panel, int nivel = 2)
        {
            // En una UI optimizada, la elevación se representa mejor con un borde sutil
            // en lugar de sombras multi-capa costosas.
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = ObtenerRectanguloRedondeado(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 15))
                using (Pen pen = new Pen(Color.FromArgb(30, 0, 0, 0), 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
        }

        #endregion
    }
}
