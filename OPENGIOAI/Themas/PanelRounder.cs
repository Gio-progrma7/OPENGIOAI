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
            bool agregarSombra = true)
        {
            if (panel == null)
                throw new ArgumentNullException(nameof(panel));

            // Color por defecto
            Color colorBorde = borderColor ?? Color.FromArgb(200, 200, 200);

            // Configurar el panel para doble buffering (evita parpadeo)
            typeof(Panel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic,
                null, panel, new object[] { true });

            // Variable para almacenar la región actual
            GraphicsPath regionPath = null;

            // Evento Paint para dibujar el panel redondeado
            panel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;

                // Configuración de máxima calidad para renderizado profesional
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Rectángulo del panel
                Rectangle rectPanel = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

                // Crear path para la región
                using (GraphicsPath pathRegion = ObtenerRectanguloRedondeado(rectPanel, borderRadius))
                {
                    // Establecer región redondeada
                    panel.Region = new Region(pathRegion);

                    // Dibujar sombra si está habilitada
                    if (agregarSombra)
                    {
                        DibujarSombraProfesional(g, pathRegion, panel.ClientRectangle);
                    }

                    // Dibujar fondo del panel
                    using (SolidBrush brush = new SolidBrush(panel.BackColor))
                    {
                        g.FillPath(brush, pathRegion);
                    }

                    // Dibujar borde si borderSize > 0
                    if (borderSize > 0)
                    {
                        Rectangle rectBorde = new Rectangle(
                            borderSize / 2,
                            borderSize / 2,
                            panel.Width - borderSize - 1,
                            panel.Height - borderSize - 1
                        );

                        using (GraphicsPath pathBorde = ObtenerRectanguloRedondeado(rectBorde, borderRadius))
                        using (Pen pen = new Pen(colorBorde, borderSize))
                        {
                            pen.Alignment = PenAlignment.Inset;
                            pen.LineJoin = LineJoin.Round;
                            pen.StartCap = LineCap.Round;
                            pen.EndCap = LineCap.Round;
                            g.DrawPath(pen, pathBorde);
                        }
                    }
                }
            };

            // Evento Resize para actualizar la región
            panel.Resize += (s, e) =>
            {
                panel.Invalidate();
            };

            // Forzar primer dibujado
            panel.Invalidate();
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
        /// Dibuja una sombra profesional alrededor del panel
        /// </summary>
        private static void DibujarSombraProfesional(Graphics g, GraphicsPath path, Rectangle bounds)
        {
            // Sombra con múltiples capas para efecto profesional
            for (int i = 1; i <= 5; i++)
            {
                int opacity = 15 - (i * 2); // Opacidad decreciente
                using (Pen penSombra = new Pen(Color.FromArgb(opacity, 0, 0, 0), i))
                {
                    using (GraphicsPath shadowPath = (GraphicsPath)path.Clone())
                    {
                        using (Matrix matrix = new Matrix())
                        {
                            matrix.Translate(i * 0.5f, i * 0.5f);
                            shadowPath.Transform(matrix);
                            g.DrawPath(penSombra, shadowPath);
                        }
                    }
                }
            }
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
        /// Aplica efecto de elevación (más sombra) al panel
        /// </summary>
        public static void AplicarElevacion(this Panel panel, int nivel = 2)
        {
            // nivel 1 = sombra ligera, nivel 3 = sombra profunda
            panel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle bounds = panel.ClientRectangle;
                using (GraphicsPath path = ObtenerRectanguloRedondeado(bounds, 15))
                {
                    // Sombra con elevación
                    for (int i = 1; i <= nivel * 3; i++)
                    {
                        int opacity = Math.Max(5, 20 - (i * 2));
                        using (Pen penSombra = new Pen(Color.FromArgb(opacity, 0, 0, 0), i))
                        {
                            using (GraphicsPath shadowPath = (GraphicsPath)path.Clone())
                            {
                                using (Matrix matrix = new Matrix())
                                {
                                    matrix.Translate(i * 0.3f, i * 0.6f);
                                    shadowPath.Transform(matrix);
                                    g.DrawPath(penSombra, shadowPath);
                                }
                            }
                        }
                    }
                }
            };
        }

        #endregion
    }
}
