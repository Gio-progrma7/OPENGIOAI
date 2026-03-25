using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Clase con métodos para redondear Botones y aplicar estilos modernos
    /// </summary>
    public static class ButtonRounder
    {
        #region Método Principal

        /// <summary>
        /// Redondea un Button con diseño moderno y profesional
        /// </summary>
        /// <param name="button">Button a redondear</param>
        /// <param name="borderRadius">Radio de las esquinas (default: 10)</param>
        /// <param name="colorNormal">Color normal del botón</param>
        /// <param name="colorHover">Color al pasar el mouse</param>
        /// <param name="colorClick">Color al hacer click</param>
        /// <param name="colorTexto">Color del texto</param>
        /// <param name="agregarSombra">Agregar sombra profesional (default: true)</param>
        public static void RedondearBoton(
            this Button button,
            int borderRadius = 10,
            Color? colorNormal = null,
            Color? colorHover = null,
            Color? colorClick = null,
            Color? colorTexto = null,
            bool agregarSombra = true)
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));

            // Colores por defecto (estilo Primary)
            Color normal = colorNormal ?? Color.FromArgb(64, 158, 255);
            Color hover = colorHover ?? Color.FromArgb(41, 128, 185);
            Color click = colorClick ?? Color.FromArgb(30, 108, 155);
            Color texto = colorTexto ?? Color.White;

            // Estado actual del botón
            EstadoBoton estadoActual = EstadoBoton.Normal;

            // Configurar propiedades básicas del botón
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = normal;
            button.ForeColor = texto;
            button.Cursor = Cursors.Hand;
            button.Font = new Font(button.Font.FontFamily, button.Font.Size, FontStyle.Bold);

            // Evento Paint para dibujar el botón redondeado
            button.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;

                // Configuración de máxima calidad
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Determinar color según estado
                Color colorActual = normal;
                switch (estadoActual)
                {
                    case EstadoBoton.Hover:
                        colorActual = hover;
                        break;
                    case EstadoBoton.Click:
                        colorActual = click;
                        break;
                }

                // Rectángulo del botón
                Rectangle rect = new Rectangle(0, 0, button.Width - 1, button.Height - 1);

                using (GraphicsPath path = ObtenerRectanguloRedondeado(rect, borderRadius))
                {
                    // Establecer región
                    button.Region = new Region(path);

                    // Dibujar sombra si está habilitada y no está presionado
                    if (agregarSombra && estadoActual != EstadoBoton.Click)
                    {
                        DibujarSombraBoton(g, path, rect);
                    }

                    // Dibujar fondo con gradiente sutil
                    using (LinearGradientBrush brush = new LinearGradientBrush(
                        rect,
                        colorActual,
                        AjustarBrillo(colorActual, estadoActual == EstadoBoton.Click ? -20 : -10),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(brush, path);
                    }

                    // Dibujar borde sutil interno (efecto de profundidad)
                    if (estadoActual != EstadoBoton.Click)
                    {
                        Rectangle rectBorde = new Rectangle(1, 1, button.Width - 3, button.Height - 3);
                        using (GraphicsPath pathBorde = ObtenerRectanguloRedondeado(rectBorde, borderRadius - 1))
                        using (Pen penBorde = new Pen(Color.FromArgb(30, 255, 255, 255), 1))
                        {
                            g.DrawPath(penBorde, pathBorde);
                        }
                    }
                }

                // Dibujar texto centrado
                TextRenderer.DrawText(
                    g,
                    button.Text,
                    button.Font,
                    button.ClientRectangle,
                    button.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            };

            // Eventos de interacción
            button.MouseEnter += (s, e) =>
            {
                estadoActual = EstadoBoton.Hover;
                button.Invalidate();
            };

            button.MouseLeave += (s, e) =>
            {
                estadoActual = EstadoBoton.Normal;
                button.Invalidate();
            };

            button.MouseDown += (s, e) =>
            {
                estadoActual = EstadoBoton.Click;
                button.Invalidate();
            };

            button.MouseUp += (s, e) =>
            {
                estadoActual = button.ClientRectangle.Contains(button.PointToClient(Cursor.Position))
                    ? EstadoBoton.Hover
                    : EstadoBoton.Normal;
                button.Invalidate();
            };

            // Forzar primer dibujado
            button.Invalidate();
        }

        #endregion

        #region Sobrecarga Simplificada

        /// <summary>
        /// Redondea un Button con valores predeterminados
        /// </summary>
        public static void Redondear(this Button button)
        {
            RedondearBoton(button);
        }

        #endregion

        #region Estilos Predefinidos

        /// <summary>
        /// Estilo Primary - Azul moderno (más usado)
        /// </summary>
        public static void AplicarEstiloPrimary(this Button button, int borderRadius = 10)
        {
            RedondearBoton(
                button,
                borderRadius,
                colorNormal: Color.FromArgb(64, 158, 255),
                colorHover: Color.FromArgb(41, 128, 185),
                colorClick: Color.FromArgb(30, 108, 155),
                colorTexto: Color.White
            );
        }

        /// <summary>
        /// Estilo Success - Verde (confirmar, aceptar)
        /// </summary>
        public static void AplicarEstiloSuccess(this Button button, int borderRadius = 10)
        {
            RedondearBoton(
                button,
                borderRadius,
                colorNormal: Color.FromArgb(46, 204, 113),
                colorHover: Color.FromArgb(39, 174, 96),
                colorClick: Color.FromArgb(30, 140, 76),
                colorTexto: Color.White
            );
        }

        /// <summary>
        /// Estilo Danger - Rojo (eliminar, cancelar)
        /// </summary>
        public static void AplicarEstiloDanger(this Button button, int borderRadius = 10)
        {
            RedondearBoton(
                button,
                borderRadius,
                colorNormal: Color.FromArgb(231, 76, 60),
                colorHover: Color.FromArgb(192, 57, 43),
                colorClick: Color.FromArgb(160, 40, 30),
                colorTexto: Color.White
            );
        }

        /// <summary>
        /// Estilo Warning - Naranja (advertencia)
        /// </summary>
        public static void AplicarEstiloWarning(this Button button, int borderRadius = 10)
        {
            RedondearBoton(
                button,
                borderRadius,
                colorNormal: Color.FromArgb(243, 156, 18),
                colorHover: Color.FromArgb(211, 134, 15),
                colorClick: Color.FromArgb(180, 110, 10),
                colorTexto: Color.White
            );
        }

        /// <summary>
        /// Estilo Info - Cyan/Celeste (información)
        /// </summary>
        public static void AplicarEstiloInfo(this Button button, int borderRadius = 10)
        {
            RedondearBoton(
                button,
                borderRadius,
                colorNormal: Color.FromArgb(52, 152, 219),
                colorHover: Color.FromArgb(41, 128, 185),
                colorClick: Color.FromArgb(30, 100, 150),
                colorTexto: Color.White
            );
        }

        /// <summary>
        /// Estilo Oscuro - Dark Theme
        /// </summary>
        public static void AplicarEstiloOscuro(this Button button, int borderRadius = 10)
        {
            RedondearBoton(
                button,
                borderRadius,
                colorNormal: Color.FromArgb(52, 73, 94),
                colorHover: Color.FromArgb(44, 62, 80),
                colorClick: Color.FromArgb(35, 50, 65),
                colorTexto: Color.White
            );
        }

        /// <summary>
        /// Estilo Outline - Solo borde, fondo transparente
        /// </summary>
        public static void AplicarEstiloOutline(
            this Button button, 
            Color colorBorde, 
            int borderRadius = 10)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.Transparent;
            button.ForeColor = colorBorde;
            button.Cursor = Cursors.Hand;

            EstadoBoton estado = EstadoBoton.Normal;

            button.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(1, 1, button.Width - 3, button.Height - 3);
                using (GraphicsPath path = ObtenerRectanguloRedondeado(rect, borderRadius))
                {
                    button.Region = new Region(path);

                    // Fondo al hacer hover
                    if (estado == EstadoBoton.Hover || estado == EstadoBoton.Click)
                    {
                        using (SolidBrush brush = new SolidBrush(
                            Color.FromArgb(estado == EstadoBoton.Click ? 40 : 20, colorBorde)))
                        {
                            g.FillPath(brush, path);
                        }
                    }

                    // Borde
                    using (Pen pen = new Pen(colorBorde, 2))
                    {
                        pen.Alignment = PenAlignment.Inset;
                        g.DrawPath(pen, path);
                    }
                }

                TextRenderer.DrawText(g, button.Text, button.Font,
                    button.ClientRectangle, button.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            button.MouseEnter += (s, e) => { estado = EstadoBoton.Hover; button.Invalidate(); };
            button.MouseLeave += (s, e) => { estado = EstadoBoton.Normal; button.Invalidate(); };
            button.MouseDown += (s, e) => { estado = EstadoBoton.Click; button.Invalidate(); };
            button.MouseUp += (s, e) => { estado = EstadoBoton.Hover; button.Invalidate(); };
        }

        /// <summary>
        /// Estilo Ghost - Fondo transparente, texto con color
        /// </summary>
        public static void AplicarEstiloGhost(this Button button, Color colorTexto, int borderRadius = 10)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.Transparent;
            button.ForeColor = colorTexto;
            button.Cursor = Cursors.Hand;

            EstadoBoton estado = EstadoBoton.Normal;

            button.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = button.ClientRectangle;
                using (GraphicsPath path = ObtenerRectanguloRedondeado(rect, borderRadius))
                {
                    button.Region = new Region(path);

                    // Fondo sutil al hacer hover
                    if (estado == EstadoBoton.Hover || estado == EstadoBoton.Click)
                    {
                        using (SolidBrush brush = new SolidBrush(
                            Color.FromArgb(estado == EstadoBoton.Click ? 30 : 15, colorTexto)))
                        {
                            g.FillPath(brush, path);
                        }
                    }
                }

                TextRenderer.DrawText(g, button.Text, button.Font,
                    button.ClientRectangle, button.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            button.MouseEnter += (s, e) => { estado = EstadoBoton.Hover; button.Invalidate(); };
            button.MouseLeave += (s, e) => { estado = EstadoBoton.Normal; button.Invalidate(); };
            button.MouseDown += (s, e) => { estado = EstadoBoton.Click; button.Invalidate(); };
            button.MouseUp += (s, e) => { estado = EstadoBoton.Hover; button.Invalidate(); };
        }

        #endregion

        #region Métodos Helper Privados

        /// <summary>
        /// Genera un GraphicsPath con bordes redondeados
        /// </summary>
        private static GraphicsPath ObtenerRectanguloRedondeado(Rectangle rect, int radio)
        {
            GraphicsPath path = new GraphicsPath();

            int radioMaximo = Math.Min(rect.Width, rect.Height) / 2;
            radio = Math.Min(radio, radioMaximo);

            float diametro = radio * 2F;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diametro, diametro, 180, 90);
            path.AddArc(rect.Right - diametro, rect.Y, diametro, diametro, 270, 90);
            path.AddArc(rect.Right - diametro, rect.Bottom - diametro, diametro, diametro, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diametro, diametro, diametro, 90, 90);
            path.CloseFigure();

            return path;
        }

        /// <summary>
        /// Dibuja sombra profesional para el botón
        /// </summary>
        private static void DibujarSombraBoton(Graphics g, GraphicsPath path, Rectangle bounds)
        {
            for (int i = 1; i <= 4; i++)
            {
                int opacity = 18 - (i * 3);
                using (Pen penSombra = new Pen(Color.FromArgb(opacity, 0, 0, 0), i))
                {
                    using (GraphicsPath shadowPath = (GraphicsPath)path.Clone())
                    {
                        using (Matrix matrix = new Matrix())
                        {
                            matrix.Translate(0, i * 0.5f);
                            shadowPath.Transform(matrix);
                            g.DrawPath(penSombra, shadowPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Ajusta el brillo de un color
        /// </summary>
        private static Color AjustarBrillo(Color color, int cantidad)
        {
            int r = Math.Max(0, Math.Min(255, color.R + cantidad));
            int g = Math.Max(0, Math.Min(255, color.G + cantidad));
            int b = Math.Max(0, Math.Min(255, color.B + cantidad));
            return Color.FromArgb(color.A, r, g, b);
        }

        #endregion

        #region Métodos para Múltiples Botones

        /// <summary>
        /// Redondea múltiples Buttons a la vez
        /// </summary>
        public static void RedondearMultiples(int borderRadius, params Button[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
                return;

            foreach (var btn in buttons)
            {
                if (btn != null)
                {
                    RedondearBoton(btn, borderRadius);
                }
            }
        }

        /// <summary>
        /// Redondea todos los Buttons de un formulario
        /// </summary>
        public static void RedondearTodosEnFormulario(Form formulario, int borderRadius = 10)
        {
            if (formulario == null)
                throw new ArgumentNullException(nameof(formulario));

            foreach (Control control in formulario.Controls)
            {
                if (control is Button button)
                {
                    RedondearBoton(button, borderRadius);
                }
                else if (control.HasChildren)
                {
                    RedondearTodosEnContenedor(control, borderRadius);
                }
            }
        }

        private static void RedondearTodosEnContenedor(Control contenedor, int borderRadius)
        {
            foreach (Control control in contenedor.Controls)
            {
                if (control is Button button)
                {
                    RedondearBoton(button, borderRadius);
                }
                else if (control.HasChildren)
                {
                    RedondearTodosEnContenedor(control, borderRadius);
                }
            }
        }

        #endregion

        #region Enum de Estados

        private enum EstadoBoton
        {
            Normal,
            Hover,
            Click
        }

        #endregion
    }
}
