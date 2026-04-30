using OPENGIOAI.Entidades;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Control visual arrastrable que representa un nodo en el canvas de automatizaciones.
    /// </summary>
    public class NodoVisualControl : Control
    {
        // ── Colores por tipo de nodo ──────────────────────────────────────────
        private static readonly Color ColorDisparador  = ColorTranslator.FromHtml("#3b82f6"); // azul
        private static readonly Color ColorCondicion   = ColorTranslator.FromHtml("#f59e0b"); // ámbar
        private static readonly Color ColorAccion = ColorTranslator.FromHtml("#3660C9"); // blue
        private static readonly Color ColorFin   = ColorTranslator.FromHtml("#f87171"); // rojo

        private static Color BgCard    => EmeraldTheme.BgCard;
        private static Color BgCardHov => EmeraldTheme.IsDark
                                            ? ColorTranslator.FromHtml("#003d73")
                                            : ColorTranslator.FromHtml("#D6E8FF");
        private static Color TextMain  => EmeraldTheme.TextPrimary;
        private static readonly Color TextSub    = ColorTranslator.FromHtml("#9ca3af");

        // ── Tamaño base (sin zoom) ────────────────────────────────────────────
        public const int BaseWidth  = 200;
        public const int BaseHeight = 80;

        /// <summary>Factor de zoom actual. El canvas lo asigna al crear/reescalar el nodo.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float Zoom { get; set; } = 1.0f;

        // ── Datos del nodo ────────────────────────────────────────────────────
        public NodoAutomatizacion Datos { get; }

        // ── Estado visual ─────────────────────────────────────────────────────
        private bool _hovered    = false;
        private bool _seleccionado = false;
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Seleccionado
        {
            get => _seleccionado;
            set { _seleccionado = value; Invalidate(); }
        }

        // ── Arrastre ──────────────────────────────────────────────────────────
        private bool  _arrastrando = false;
        private Point _offsetArrastre;

        // ── Puntos de conexión (centro izquierda y centro derecha) ────────────
        public Point PuntoEntrada  => new Point(0,           Height / 2);
        public Point PuntoSalida   => new Point(Width,       Height / 2);

        public Point PuntoEntradaAbsoluto  => new Point(Left,        Top + Height / 2);
        public Point PuntoSalidaAbsoluto   => new Point(Left + Width, Top + Height / 2);

        // ── Eventos ───────────────────────────────────────────────────────────
        public event EventHandler<NodoVisualControl>? NodoSeleccionado;
        public event EventHandler<NodoVisualControl>? NodoMovido;
        public event EventHandler<NodoVisualControl>? NodoEliminado;
        public event EventHandler<NodoVisualControl>? IniciarConexion;

        // ── Constructor ───────────────────────────────────────────────────────
        public NodoVisualControl(NodoAutomatizacion datos)
        {
            Datos        = datos;
            Size         = new Size(BaseWidth, BaseHeight);
            Location     = new Point(datos.CanvasX, datos.CanvasY);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.SizeAll;
        }

        // ── Pintar ────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode        = SmoothingMode.AntiAlias;
            g.TextRenderingHint    = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color tipoColor = ObtenerColorTipo();
            Rectangle rc    = new Rectangle(0, 0, Width - 1, Height - 1);

            // Sombra
            if (_seleccionado || _hovered)
            {
                using var shadowPath = RoundedRect(new Rectangle(3, 3, Width - 3, Height - 3), 10);
                using var shadowBrush = new SolidBrush(Color.FromArgb(60, tipoColor));
                g.FillPath(shadowBrush, shadowPath);
            }

            // Fondo — más brillante si está ejecutando
            Color bgColor = Datos.Estado switch
            {
                EstadoNodo.Ejecutando  => ColorTranslator.FromHtml("#0f2a1c"),
                EstadoNodo.Completado  => ColorTranslator.FromHtml("#0a1f15"),
                EstadoNodo.Error       => ColorTranslator.FromHtml("#2a0f0f"),
                _                      => _hovered ? BgCardHov : BgCard
            };
            using var bgPath = RoundedRect(rc, 10);
            using var bgBrush = new SolidBrush(bgColor);
            g.FillPath(bgBrush, bgPath);

            // Franja de color (borde superior = tipo / estado)
            Color franjaColor = Datos.Estado switch
            {
                EstadoNodo.Ejecutando  => ColorTranslator.FromHtml("#94E6EC"),
                EstadoNodo.Completado  => ColorTranslator.FromHtml("#3660C9"),
                EstadoNodo.Error       => ColorTranslator.FromHtml("#f87171"),
                _                      => tipoColor
            };
            Rectangle franjaRect = new Rectangle(0, 0, Width - 1, 5);
            using var franjaBrush = new SolidBrush(franjaColor);
            g.FillRectangle(franjaBrush, franjaRect);

            // Borde
            float borderW = (_seleccionado || Datos.Estado == EstadoNodo.Ejecutando) ? 2.5f : 1.5f;
            Color borderColor = Datos.Estado switch
            {
                EstadoNodo.Ejecutando  => ColorTranslator.FromHtml("#94E6EC"),
                EstadoNodo.Completado  => ColorTranslator.FromHtml("#3660C9"),
                EstadoNodo.Error       => ColorTranslator.FromHtml("#f87171"),
                _                      => _seleccionado ? tipoColor : Color.FromArgb(50, tipoColor)
            };
            using var borderPen = new Pen(borderColor, borderW);
            g.DrawPath(borderPen, bgPath);

            // Icono de tipo + estado de ejecución
            string tipoLabel = Datos.TipoNodo switch
            {
                TipoNodo.Disparador => "⚡ DISPARADOR",
                TipoNodo.Condicion  => "⬡ CONDICIÓN",
                TipoNodo.Accion     => "▶ ACCIÓN",
                TipoNodo.Fin        => "■ FIN",
                _                   => "● NODO"
            };
            string estadoLabel = Datos.Estado switch
            {
                EstadoNodo.Ejecutando => "  ⏳",
                EstadoNodo.Completado => "  ✔",
                EstadoNodo.Error      => "  ✖",
                _                     => ""
            };

            using var fntTipo = new Font("Segoe UI", 6.5f, FontStyle.Bold);
            using var brTipo  = new SolidBrush(franjaColor);
            g.DrawString(tipoLabel + estadoLabel, fntTipo, brTipo, new PointF(10, 10));

            // Título
            using var fntTitulo = new Font("Segoe UI Semibold", 9f);
            using var brMain    = new SolidBrush(TextMain);
            Rectangle rcTitulo  = new Rectangle(10, 24, Width - 20, 20);
            TextRenderer.DrawText(g, Datos.Titulo, fntTitulo, rcTitulo, TextMain,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Descripción (subtexto)
            if (!string.IsNullOrWhiteSpace(Datos.Descripcion))
            {
                using var fntDesc = new Font("Segoe UI", 7.5f);
                using var brSub   = new SolidBrush(TextSub);
                Rectangle rcDesc  = new Rectangle(10, 46, Width - 20, 26);
                TextRenderer.DrawText(g, Datos.Descripcion, fntDesc, rcDesc, TextSub,
                    TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.WordBreak);
            }

            // Punto de conexión salida (círculo derecho)
            DrawConPoint(g, tipoColor, PuntoSalida,  salida: true);
            DrawConPoint(g, tipoColor, PuntoEntrada, salida: false);
        }

        private void DrawConPoint(Graphics g, Color c, Point pt, bool salida)
        {
            int r = 5;
            Rectangle rc = new Rectangle(pt.X - r, pt.Y - r, r * 2, r * 2);
            using var fill = new SolidBrush(Color.FromArgb(200, c));
            using var border = new Pen(c, 1.5f);
            g.FillEllipse(fill, rc);
            g.DrawEllipse(border, rc);
        }

        // ── Arrastre ──────────────────────────────────────────────────────────
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                // Si clic cerca del punto de salida → iniciar conexión
                if (e.X >= Width - 14 && Math.Abs(e.Y - Height / 2) <= 10)
                {
                    IniciarConexion?.Invoke(this, this);
                    return;
                }

                _arrastrando    = true;
                _offsetArrastre = e.Location;
                NodoSeleccionado?.Invoke(this, this);
                BringToFront();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_arrastrando && e.Button == MouseButtons.Left)
            {
                int newX = Math.Max(0, Left + e.X - _offsetArrastre.X);
                int newY = Math.Max(0, Top  + e.Y - _offsetArrastre.Y);
                Location = new Point(newX, newY);
                // Guardar posición lógica (sin zoom) para que sea correcta al guardar
                Datos.CanvasX = (int)(newX / Zoom);
                Datos.CanvasY = (int)(newY / Zoom);
                Parent?.Invalidate(); // redibuja flechas bezier
                NodoMovido?.Invoke(this, this);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _arrastrando = false;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            Invalidate();
        }

        // Doble clic → editar
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            NodoSeleccionado?.Invoke(this, this);
        }

        // ── API pública ───────────────────────────────────────────────────────
        public void ActualizarEstado(EstadoNodo estado)
        {
            Datos.Estado = estado;
            if (InvokeRequired) BeginInvoke(Invalidate);
            else Invalidate();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private Color ObtenerColorTipo() => Datos.TipoNodo switch
        {
            TipoNodo.Disparador => ColorDisparador,
            TipoNodo.Condicion  => ColorCondicion,
            TipoNodo.Accion     => ColorAccion,
            TipoNodo.Fin        => ColorFin,
            _                   => ColorAccion
        };

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
    }
}
