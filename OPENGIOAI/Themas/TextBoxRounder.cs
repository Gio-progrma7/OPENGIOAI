using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Clase con métodos para redondear TextBox y aplicar estilos modernos
    /// </summary>
    public static class TextBoxRounder
    {
        #region Método Principal

        /// <summary>
        /// Redondea un TextBox con diseño moderno
        /// </summary>
        /// <param name="textBox">TextBox a redondear</param>
        /// <param name="borderRadius">Radio de las esquinas (default: 10)</param>
        /// <param name="borderColor">Color del borde (default: azul)</param>
        /// <param name="borderSize">Grosor del borde (default: 2)</param>
        /// <param name="focusColor">Color del borde al enfocar (default: azul oscuro)</param>
        /// <param name="agregarSombra">Agregar sombra sutil (default: true)</param>
        public static void RedondearTextBox(
            this TextBox textBox,
            int borderRadius = 10,
            Color? borderColor = null,
            int borderSize = 2,
            Color? focusColor = null,
            bool agregarSombra = true)
        {
            if (textBox == null)
                throw new ArgumentNullException(nameof(textBox));

            // Colores por defecto
            Color colorBorde = borderColor ?? Color.FromArgb(64, 158, 255);
            Color colorFoco = focusColor ?? Color.FromArgb(41, 128, 185);

            // Crear panel contenedor
            Panel panelContenedor = new Panel
            {
                Size = new Size(textBox.Width, textBox.Height),
                Location = textBox.Location,
                BackColor = textBox.BackColor,
                Padding = new Padding(12, 8, 12, 8), // Padding más profesional
                Tag = "RoundedContainer" // Para identificarlo después
            };

            // Configurar TextBox
            textBox.BorderStyle = BorderStyle.None;
            textBox.Location = new Point(0, 0);
            textBox.Dock = DockStyle.Fill;

            // Reemplazar TextBox con el panel en el formulario
            Control parent = textBox.Parent;
            int tabIndex = textBox.TabIndex;
            string name = textBox.Name;

            if (parent != null)
            {
                int index = parent.Controls.GetChildIndex(textBox);
                parent.Controls.Remove(textBox);
                panelContenedor.Controls.Add(textBox);
                parent.Controls.Add(panelContenedor);
                parent.Controls.SetChildIndex(panelContenedor, index);
            }
            else
            {
                panelContenedor.Controls.Add(textBox);
            }

            panelContenedor.Name = name;
            panelContenedor.TabIndex = tabIndex;

            // Variables para el estado
            bool estaEnfocado = false;

            // Evento Paint para dibujar bordes redondeados
            panelContenedor.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;

                // Configuración de alta calidad para renderizado profesional
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Ajustar rectángulo para que el borde no se corte
                int ajuste = borderSize;
                Rectangle rectBorde = new Rectangle(
                    ajuste,
                    ajuste,
                    panelContenedor.Width - (borderSize * 2) - 1,
                    panelContenedor.Height - (borderSize * 2) - 1
                );

                Color colorActual = estaEnfocado ? colorFoco : colorBorde;

                using (GraphicsPath path = ObtenerRectanguloRedondeado(rectBorde, borderRadius))
                {
                    // Establecer región del panel
                    Rectangle rectRegion = new Rectangle(0, 0, panelContenedor.Width - 1, panelContenedor.Height - 1);
                    using (GraphicsPath pathRegion = ObtenerRectanguloRedondeado(rectRegion, borderRadius + 1))
                    {
                        panelContenedor.Region = new Region(pathRegion);
                    }

                    // Dibujar sombra sutil si está habilitada
                    if (agregarSombra && !estaEnfocado)
                    {
                        using (GraphicsPath pathSombra = ObtenerRectanguloRedondeado(
                            new Rectangle(rectBorde.X + 1, rectBorde.Y + 2, rectBorde.Width, rectBorde.Height),
                            borderRadius))
                        {
                            using (Pen penSombra = new Pen(Color.FromArgb(20, 0, 0, 0), borderSize))
                            {
                                penSombra.Alignment = PenAlignment.Center;
                                g.DrawPath(penSombra, pathSombra);
                            }
                        }
                    }

                    // Dibujar fondo con el color del panel
                    using (SolidBrush brush = new SolidBrush(panelContenedor.BackColor))
                    {
                        g.FillPath(brush, path);
                    }

                    // Dibujar borde con configuración profesional
                    using (Pen pen = new Pen(colorActual, borderSize))
                    {
                        pen.Alignment = PenAlignment.Center;
                        pen.LineJoin = LineJoin.Round; // Uniones redondeadas profesionales
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        g.DrawPath(pen, path);
                    }
                }
            };

            // Eventos de foco
            textBox.Enter += (s, e) =>
            {
                estaEnfocado = true;
                panelContenedor.Invalidate();
            };

            textBox.Leave += (s, e) =>
            {
                estaEnfocado = false;
                panelContenedor.Invalidate();
            };

            // Actualizar altura si es necesario
            if (!textBox.Multiline)
            {
                int alturaNecesaria = textBox.Font.Height + panelContenedor.Padding.Top + panelContenedor.Padding.Bottom;
                panelContenedor.Height = alturaNecesaria;
            }
        }

        #endregion

        #region Sobrecarga Simplificada

        /// <summary>
        /// Redondea un TextBox con valores predeterminados
        /// </summary>
        public static void Redondear(this TextBox textBox)
        {
            RedondearTextBox(textBox);
        }

        #endregion

        #region Variantes con Estilos Predefinidos

        /// <summary>
        /// Aplica estilo moderno azul (Primary)
        /// </summary>
        public static void AplicarEstiloPrimary(this TextBox textBox, int borderRadius = 12)
        {
            RedondearTextBox(
                textBox,
                borderRadius,
                Color.FromArgb(64, 158, 255),
                2,
                Color.FromArgb(41, 128, 185)
            );
        }

        /// <summary>
        /// Aplica estilo verde (Success)
        /// </summary>
        public static void AplicarEstiloSuccess(this TextBox textBox, int borderRadius = 12)
        {
            RedondearTextBox(
                textBox,
                borderRadius,
                Color.FromArgb(46, 204, 113),
                2,
                Color.FromArgb(39, 174, 96)
            );
        }

        /// <summary>
        /// Aplica estilo rojo (Danger)
        /// </summary>
        public static void AplicarEstiloDanger(this TextBox textBox, int borderRadius = 12)
        {
            RedondearTextBox(
                textBox,
                borderRadius,
                Color.FromArgb(231, 76, 60),
                2,
                Color.FromArgb(192, 57, 43)
            );
        }

        /// <summary>
        /// Aplica estilo oscuro (Dark Theme)
        /// </summary>
        public static void AplicarEstiloOscuro(this TextBox textBox, int borderRadius = 12)
        {
            textBox.BackColor = Color.FromArgb(45, 45, 48);
            textBox.ForeColor = Color.White;

            RedondearTextBox(
                textBox,
                borderRadius,
                Color.FromArgb(70, 70, 73),
                1,
                Color.FromArgb(0, 122, 204)
            );
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

            // Crear path con curvas suaves y profesionales
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

        #endregion

        #region Método para Aplicar a Múltiples TextBoxes

        /// <summary>
        /// Redondea múltiples TextBox a la vez
        /// </summary>
        public static void RedondearMultiples(int borderRadius, params TextBox[] textBoxes)
        {
            if (textBoxes == null || textBoxes.Length == 0)
                return;

            foreach (var txt in textBoxes)
            {
                if (txt != null)
                {
                    RedondearTextBox(txt, borderRadius);
                }
            }
        }

        /// <summary>
        /// Redondea todos los TextBox de un formulario
        /// </summary>
        public static void RedondearTodosEnFormulario(Form formulario, int borderRadius = 10)
        {
            if (formulario == null)
                throw new ArgumentNullException(nameof(formulario));

            foreach (Control control in formulario.Controls)
            {
                if (control is TextBox textBox)
                {
                    RedondearTextBox(textBox, borderRadius);
                }
                else if (control.HasChildren)
                {
                    RedondearTodosEnPanel(control, borderRadius);
                }
            }
        }

        /// <summary>
        /// Redondea todos los TextBox dentro de un contenedor (Panel, GroupBox, etc.)
        /// </summary>
        private static void RedondearTodosEnPanel(Control contenedor, int borderRadius)
        {
            foreach (Control control in contenedor.Controls)
            {
                if (control is TextBox textBox)
                {
                    RedondearTextBox(textBox, borderRadius);
                }
                else if (control.HasChildren)
                {
                    RedondearTodosEnPanel(control, borderRadius);
                }
            }
        }

        #endregion



        public static void AjustarAlturaRichTextBox(RichTextBox rtb)
        {

            if (rtb.Parent is not Panel panelContenedor)
                return;

            int margen = 10;
            int alturaMinima = 50;
            int alturaMaxima = 250;

            Size tamañoTexto = TextRenderer.MeasureText(
                rtb.Text + " ",
                rtb.Font,
                new Size(rtb.Width, int.MaxValue),
                TextFormatFlags.WordBreak);

            int nuevaAltura = tamañoTexto.Height + margen;
            nuevaAltura = Math.Max(alturaMinima, Math.Min(nuevaAltura, alturaMaxima));

            panelContenedor.Height = nuevaAltura;

            rtb.ScrollBars = nuevaAltura >= alturaMaxima
                ? RichTextBoxScrollBars.Vertical
                : RichTextBoxScrollBars.None;



        }

        public static void AjustarAlturaConRedondeo(
        RichTextBox rtb,
        int borderRadius = 10,
        int paddingInterno = 10,
        int alturaMinima = 50,
        int alturaMaxima = 100)
        {
            if (rtb?.Parent is not Panel panelContenedor)
                return;

            panelContenedor.SuspendLayout();

            // 🔹 Medir texto real
            Size tamañoTexto = TextRenderer.MeasureText(
                rtb.Text + " ",
                rtb.Font,
                new Size(rtb.Width, int.MaxValue),
                TextFormatFlags.WordBreak);

            int nuevaAltura = tamañoTexto.Height + paddingInterno;
            nuevaAltura = Math.Max(alturaMinima, Math.Min(nuevaAltura, alturaMaxima));

            panelContenedor.Height = nuevaAltura;

            // 🔹 Scroll automático si supera máximo
            rtb.ScrollBars = nuevaAltura >= alturaMaxima
                ? RichTextBoxScrollBars.Vertical
                : RichTextBoxScrollBars.None;

            panelContenedor.ResumeLayout();

            // 🔥 Recalcular región redondeada correctamente
            using (GraphicsPath path = ObtenerPath(
                new Rectangle(0, 0, panelContenedor.Width, panelContenedor.Height),
                borderRadius))
            {
                panelContenedor.Region = new Region(path);
            }

            panelContenedor.Invalidate();
        }

        private static GraphicsPath ObtenerPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

    }
}
