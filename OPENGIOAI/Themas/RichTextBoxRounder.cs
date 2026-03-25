using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Themas
{
    public static class RichTextBoxRounder
    {
        public static void RedondearRichTextBox(
            this RichTextBox richTextBox,
            int borderRadius = 10,
            Color? borderColor = null,
            int borderSize = 2,
            Color? focusColor = null,
            bool agregarSombra = true)
        {
            if (richTextBox == null)
                throw new ArgumentNullException(nameof(richTextBox));

            Color colorBorde = borderColor ?? Color.FromArgb(64, 158, 255);
            Color colorFoco = focusColor ?? Color.FromArgb(41, 128, 185);

            Panel panelContenedor = new Panel
            {
                Size = richTextBox.Size,
                Location = richTextBox.Location,
                BackColor = richTextBox.BackColor,
                Padding = new Padding(10),
            };

            // Configurar RichTextBox
            richTextBox.BorderStyle = BorderStyle.None;
            richTextBox.Dock = DockStyle.Fill;
            richTextBox.Multiline = true;
            richTextBox.ScrollBars = RichTextBoxScrollBars.Vertical;

            Control parent = richTextBox.Parent;
            int index = parent.Controls.GetChildIndex(richTextBox);

            parent.Controls.Remove(richTextBox);
            panelContenedor.Controls.Add(richTextBox);
            parent.Controls.Add(panelContenedor);
            parent.Controls.SetChildIndex(panelContenedor, index);

            bool enfocado = false;

            panelContenedor.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(
                    borderSize,
                    borderSize,
                    panelContenedor.Width - borderSize * 2 - 1,
                    panelContenedor.Height - borderSize * 2 - 1
                );

                Color colorActual = enfocado ? colorFoco : colorBorde;

                using (GraphicsPath path = ObtenerPath(rect, borderRadius))
                {
                    panelContenedor.Region = new Region(ObtenerPath(
                        new Rectangle(0, 0, panelContenedor.Width, panelContenedor.Height),
                        borderRadius + 1));

                    if (agregarSombra && !enfocado)
                    {
                        using Pen sombra = new Pen(Color.FromArgb(20, 0, 0, 0), borderSize);
                        g.DrawPath(sombra, path);
                    }

                    using Pen pen = new Pen(colorActual, borderSize);
                    g.DrawPath(pen, path);
                }
            };

            richTextBox.Enter += (s, e) =>
            {
                enfocado = true;
                panelContenedor.Invalidate();
            };

            richTextBox.Leave += (s, e) =>
            {
                enfocado = false;
                panelContenedor.Invalidate();
            };
        }

        private static GraphicsPath ObtenerPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }
    }

}
