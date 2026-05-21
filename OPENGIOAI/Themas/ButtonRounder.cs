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
            bool agregarSombra = false) // Sombra desactivada por defecto para mejor rendimiento
        {
            if (button == null)
                throw new ArgumentNullException(nameof(button));

            // Colores por defecto (estilo Primary)
            Color normal = colorNormal ?? Color.FromArgb(64, 158, 255);
            Color hover = colorHover ?? Color.FromArgb(41, 128, 185);
            Color click = colorClick ?? Color.FromArgb(30, 108, 155);
            Color texto = colorTexto ?? Color.White;

            // Configurar propiedades básicas del botón para máxima fluidez
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = normal;
            button.ForeColor = texto;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI Semibold", button.Font.Size);

            button.FlatAppearance.MouseOverBackColor = hover;
            button.FlatAppearance.MouseDownBackColor = click;

            // Redondeo simple usando Region (solo se ejecuta al cambiar el tamaño)
            button.Resize += (s, e) =>
            {
                if (button.Width > 0 && button.Height > 0)
                {
                    using (GraphicsPath path = ObtenerRectanguloRedondeado(button.ClientRectangle, borderRadius))
                    {
                        button.Region = new Region(path);
                    }
                }
            };

            if (button.Width > 0 && button.Height > 0)
            {
                using (GraphicsPath path = ObtenerRectanguloRedondeado(button.ClientRectangle, borderRadius))
                {
                    button.Region = new Region(path);
                }
            }
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
        /// Estilo Outline - Simulado con propiedades nativas
        /// </summary>
        public static void AplicarEstiloOutline(
            this Button button, 
            Color colorBorde, 
            int borderRadius = 10)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = colorBorde;
            button.BackColor = Color.Transparent;
            button.ForeColor = colorBorde;
            button.Cursor = Cursors.Hand;

            // Redondeo vía Region para evitar Paint manual
            button.Resize += (s, e) =>
            {
                if (button.Width > 0 && button.Height > 0)
                {
                    using (GraphicsPath path = ObtenerRectanguloRedondeado(button.ClientRectangle, borderRadius))
                        button.Region = new Region(path);
                }
            };
            
            if (button.Width > 0 && button.Height > 0)
            {
                using (GraphicsPath path = ObtenerRectanguloRedondeado(button.ClientRectangle, borderRadius))
                    button.Region = new Region(path);
            }
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

            // Redondeo vía Region
            button.Resize += (s, e) =>
            {
                if (button.Width > 0 && button.Height > 0)
                {
                    using (GraphicsPath path = ObtenerRectanguloRedondeado(button.ClientRectangle, borderRadius))
                        button.Region = new Region(path);
                }
            };
            
            if (button.Width > 0 && button.Height > 0)
            {
                using (GraphicsPath path = ObtenerRectanguloRedondeado(button.ClientRectangle, borderRadius))
                    button.Region = new Region(path);
            }
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
        /// Dibuja sombra profesional para el botón (Eliminado por optimización)
        /// </summary>
        private static void DibujarSombraBoton(Graphics g, GraphicsPath path, Rectangle bounds)
        {
            // Método vacío para mantener compatibilidad pero sin costo de rendimiento
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
