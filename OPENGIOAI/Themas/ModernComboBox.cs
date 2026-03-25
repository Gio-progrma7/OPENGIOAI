using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace OPENGIOAI.Themas
{
    public class ModernComboBox : ComboBox
    {
        // Propiedades configurables
        private Color _borderColor = Color.FromArgb(180, 180, 180);
        private Color _focusBorderColor = Color.FromArgb(0, 120, 215);
        private int _borderSize = 2; // Un poco más grueso suele verse mejor

        public ModernComboBox()
        {
            // Optimizaciones de renderizado
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.White;
            ForeColor = Color.Black;
            ItemHeight = 28; // Un poco más de aire
        }

        // Propiedades públicas por si quieres cambiarlas desde el diseñador
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        // Propiedades públicas por si quieres cambiarlas desde el diseñador
        public Color BorderColor { get => _borderColor; set { _borderColor = value; Invalidate(); } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FocusBorderColor { get => _focusBorderColor; set { _focusBorderColor = value; Invalidate(); } }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            // Suavizado de bordes
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            // Colores limpios
            Color bgColor = isSelected ? _focusBorderColor : BackColor;
            Color textColor = isSelected ? Color.White : ForeColor;

            // Dibujar fondo del item
            using (var brush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // Dibujar texto con un pequeño margen (Padding)
            var textRect = new Rectangle(e.Bounds.X + 5, e.Bounds.Y, e.Bounds.Width - 5, e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                GetItemText(Items[e.Index]),
                e.Font,
                textRect,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            );

            // Omitimos e.DrawFocusRectangle() para un look más limpio (flat)
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Limpiamos el fondo
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            // Dibujamos el texto del elemento seleccionado en el control cerrado
            if (SelectedIndex >= 0)
            {
                var textRect = new Rectangle(5, 0, Width - 30, Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }

            // Dibujar el triángulo del DropDown (estilo moderno)
            DrawArrow(e.Graphics);

            // Dibujar Borde
            Color currentBorder = Focused ? _focusBorderColor : _borderColor;
            using (var pen = new Pen(currentBorder, _borderSize))
            {
                pen.Alignment = PenAlignment.Inset;
                e.Graphics.DrawRectangle(pen, 0, 0, Width, Height);
            }
        }

        private void DrawArrow(Graphics g)
        {
            int arrowWidth = 10;
            int arrowHeight = 6;
            var rect = new Rectangle(Width - 25, (Height - arrowHeight) / 2, arrowWidth, arrowHeight);

            using (var pen = new Pen(_borderColor, 2))
            {
                Point[] points = {
                    new Point(rect.X, rect.Y),
                    new Point(rect.X + (arrowWidth / 2), rect.Y + arrowHeight),
                    new Point(rect.X + arrowWidth, rect.Y)
                };
                g.DrawLines(pen, points);
            }
        }

        // Forzar repintado en eventos clave
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Invalidate(); }
        protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }
    }
}