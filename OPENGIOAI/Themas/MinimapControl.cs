using OPENGIOAI.Entidades;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace OPENGIOAI.Themas
{
    public class MinimapControl : Control
    {
        private readonly Panel _wrapper;
        private readonly CanvasAutomatizacion _canvas;
        private bool _isDragging = false;
        private readonly Timer _updateTimer;

        public MinimapControl(Panel wrapper, CanvasAutomatizacion canvas)
        {
            _wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            
            Size = new Size(160, 100);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            // Suscribir eventos del wrapper para reposicionarse y redibujarse
            _wrapper.Scroll += (s, e) => { Reposicionar(); Invalidate(); };
            _wrapper.Resize += (s, e) => { Reposicionar(); Invalidate(); };
            
            // Timer para actualizar periódicamente por si cambian estados de nodos
            _updateTimer = new Timer { Interval = 150 };
            _updateTimer.Tick += (s, e) => Invalidate();
            _updateTimer.Start();
        }

        public void Reposicionar()
        {
            if (_wrapper == null || IsDisposed) return;
            
            // Colocar en la esquina inferior derecha del viewport visible del wrapper
            int x = _wrapper.ClientSize.Width - Width - 20 - _wrapper.AutoScrollPosition.X;
            int y = _wrapper.ClientSize.Height - Height - 20 - _wrapper.AutoScrollPosition.Y;
            
            if (Location.X != x || Location.Y != y)
            {
                Location = new Point(x, y);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Dimensiones
            float scaleX = (float)Width / _canvas.Width;
            float scaleY = (float)Height / _canvas.Height;

            // 1. Dibujar fondo glassmorphism
            Rectangle rc = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundedRect(rc, 8))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(200, 5, 10, 18)))
                {
                    g.FillPath(bgBrush, path);
                }
                using (var borderPen = new Pen(Color.FromArgb(80, 52, 211, 153), 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // 2. Dibujar nodos miniatura
            foreach (var ctrl in _canvas.Nodos)
            {
                var bounds = ctrl.Bounds;
                float nx = bounds.X * scaleX;
                float ny = bounds.Y * scaleY;
                float nw = bounds.Width * scaleX;
                float nh = bounds.Height * scaleY;

                if (nw < 3f) nw = 3f;
                if (nh < 2f) nh = 2f;

                Color c = ctrl.Datos.Estado switch
                {
                    EstadoNodo.Ejecutando => ColorTranslator.FromHtml("#94E6EC"),
                    EstadoNodo.Completado => ColorTranslator.FromHtml("#3660C9"),
                    EstadoNodo.Error      => ColorTranslator.FromHtml("#f87171"),
                    _                     => Color.FromArgb(120, 52, 211, 153)
                };

                using (var nodeBrush = new SolidBrush(Color.FromArgb(180, c.R, c.G, c.B)))
                {
                    g.FillRectangle(nodeBrush, nx, ny, nw, nh);
                }
            }

            // 3. Dibujar viewport visible (Focal Frame)
            float viewX = -_wrapper.AutoScrollPosition.X * scaleX;
            float viewY = -_wrapper.AutoScrollPosition.Y * scaleY;
            float viewW = _wrapper.ClientSize.Width * scaleX;
            float viewH = _wrapper.ClientSize.Height * scaleY;

            using (var viewPen = new Pen(Color.FromArgb(255, 52, 211, 153), 1.5f))
            {
                g.DrawRectangle(viewPen, viewX, viewY, viewW, viewH);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                ActualizarScroll(e.Location);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_isDragging && e.Button == MouseButtons.Left)
            {
                ActualizarScroll(e.Location);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDragging = false;
        }

        private void ActualizarScroll(Point pt)
        {
            float scaleX = (float)Width / _canvas.Width;
            float scaleY = (float)Height / _canvas.Height;

            // Calcular el centro deseado en coordenadas del canvas
            float targetCenterX = pt.X / scaleX;
            float targetCenterY = pt.Y / scaleY;

            // El scroll deseado en la esquina superior izquierda
            float scrollX = targetCenterX - _wrapper.ClientSize.Width / 2f;
            float scrollY = targetCenterY - _wrapper.ClientSize.Height / 2f;

            // Acotar limites
            int finalX = Math.Max(0, Math.Min(_canvas.Width - _wrapper.ClientSize.Width, (int)scrollX));
            int finalY = Math.Max(0, Math.Min(_canvas.Height - _wrapper.ClientSize.Height, (int)scrollY));

            _wrapper.AutoScrollPosition = new Point(finalX, finalY);
            Invalidate();
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,             r.Y,              d, d, 180, 90);
            path.AddArc(r.Right - d,     r.Y,              d, d, 270, 90);
            path.AddArc(r.Right - d,     r.Bottom - d,     d, d, 0,   90);
            path.AddArc(r.X,             r.Bottom - d,     d, d, 90,  90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
