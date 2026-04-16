// ============================================================
//  PanelAgentes.cs  — Barra visual de estado de agentes ARIA
//
//  Muestra 4 "píldoras" horizontales, una por agente:
//    ANALISTA | CONSTRUCTOR | GUARDIÁN | COMUNICADOR
//
//  Estados por píldora:
//    Idle      → gris neutro (esperando)
//    Active    → color vivo + pulso animado (trabajando)
//    Done      → verde suave (completado)
//    Error     → rojo suave (fallo)
//
//  Uso:
//    var panel = new PanelAgentes();
//    panel.SetEstado(FaseAgente.Analista, EstadoAgente.Active);
//    panel.SetEstado(FaseAgente.Analista, EstadoAgente.Done);
// ============================================================

using OPENGIOAI.Agentes;
using System.Drawing.Drawing2D;

namespace OPENGIOAI.Themas
{
    public enum EstadoAgente { Idle, Active, Done, Error }

    /// <summary>
    /// Barra horizontal con 4 píldoras de estado — una por agente ARIA.
    /// Control personalizado, completamente pintado en OnPaint.
    /// </summary>
    public sealed class PanelAgentes : Control
    {
        // ── Paleta ────────────────────────────────────────────────────────────
        private static readonly Color BgColor    = Color.FromArgb(15, 21, 34);
        private static readonly Color IdleColor  = Color.FromArgb(38, 46, 62);
        private static readonly Color IdleText   = Color.FromArgb(90, 105, 130);

        // Colores activos por agente
        private static readonly Color[] ActiveColors =
        {
            Color.FromArgb(59, 130, 246),   // Analista    → azul
            Color.FromArgb(16, 185, 129),   // Constructor → verde esmeralda
            Color.FromArgb(245, 158, 11),   // Guardián    → ámbar
            Color.FromArgb(139, 92, 246),   // Comunicador → violeta
        };

        private static readonly Color DoneColor  = Color.FromArgb(34, 197, 94);
        private static readonly Color ErrorColor = Color.FromArgb(239, 68, 68);
        private static readonly Color TextActive = Color.FromArgb(230, 235, 248);
        private static readonly Color TextDone   = Color.FromArgb(200, 240, 210);
        private static readonly Color TextError  = Color.FromArgb(254, 190, 190);

        // ── Paso numérico + nombre en minúsculas (diseño minimalista) ─────────
        private static readonly string[] Pasos   = { "1", "2", "3", "4" };
        private static readonly string[] Nombres = { "Analista", "Constructor", "Guardián", "Comunicador" };

        // ── Estado interno ────────────────────────────────────────────────────
        private readonly EstadoAgente[] _estados = new EstadoAgente[4];
        private int _pulseTick = 0;
        private readonly System.Windows.Forms.Timer _pulseTimer;

        // ── Constructor ───────────────────────────────────────────────────────

        public PanelAgentes()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint  |
                ControlStyles.ResizeRedraw, true);

            DoubleBuffered = true;
            BackColor      = BgColor;
            Height         = 30;
            Cursor         = Cursors.Default;

            // Timer de pulso para la animación de la píldora activa (~30 fps)
            _pulseTimer = new System.Windows.Forms.Timer { Interval = 33 };
            _pulseTimer.Tick += (_, _) =>
            {
                _pulseTick = (_pulseTick + 1) % 60;
                if (_estados.Any(e => e == EstadoAgente.Active))
                    Invalidate();
            };
            _pulseTimer.Start();
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>
        /// Actualiza el estado de un agente y dispara un redibujado.
        /// Seguro para llamar desde cualquier hilo.
        /// </summary>
        public void SetEstado(FaseAgente fase, EstadoAgente estado)
        {
            if (InvokeRequired) { Invoke(() => SetEstado(fase, estado)); return; }
            _estados[(int)fase] = estado;
            Invalidate();
        }

        /// <summary>Reinicia todos los agentes a Idle.</summary>
        public void Reset()
        {
            if (InvokeRequired) { Invoke(Reset); return; }
            for (int i = 0; i < 4; i++) _estados[i] = EstadoAgente.Idle;
            Invalidate();
        }

        // ── Pintado ───────────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Fondo
            using var bgBrush = new SolidBrush(BgColor);
            g.FillRectangle(bgBrush, ClientRectangle);

            const int totalAgentes = 4;
            const int padH = 4;   // padding horizontal exterior
            const int padV = 4;   // padding vertical
            const int gap  = 3;   // espacio entre píldoras
            int pillH = Height - padV * 2;
            int totalW = Width - padH * 2;
            int pillW  = (totalW - gap * (totalAgentes - 1)) / totalAgentes;

            for (int i = 0; i < totalAgentes; i++)
            {
                int x = padH + i * (pillW + gap);
                DibujarPasoMinimalista(g, i, new Rectangle(x, padV, pillW, pillH));
            }
        }

        /// <summary>
        /// Pinta una píldora de paso en estilo minimalista:
        /// número de paso pequeño a la izquierda + nombre del agente.
        /// Estado activo resalta con color vivo + pulso suave.
        /// </summary>
        private void DibujarPasoMinimalista(Graphics g, int idx, Rectangle r)
        {
            var estado    = _estados[idx];
            var baseColor = ObtenerColorBase(idx, estado);

            // Pulso alfa para el estado activo
            if (estado == EstadoAgente.Active)
            {
                double t    = _pulseTick / 60.0;
                double alfa = 0.70 + 0.30 * Math.Sin(t * Math.PI * 2);
                baseColor   = Color.FromArgb((int)(255 * alfa), baseColor);
            }

            // Fondo de la píldora (radio pequeño = look compacto)
            int radio = r.Height / 2;
            using var path = CrearRoundedRect(r, radio);
            using var fill = new SolidBrush(baseColor);
            g.FillPath(fill, path);

            // Borde muy sutil (solo en estados activo/completado/error)
            if (estado != EstadoAgente.Idle)
            {
                using var borderPen = new Pen(Color.FromArgb(50, 255, 255, 255), 1f);
                g.DrawPath(borderPen, path);
            }

            // ── Texto: número de paso + nombre ───────────────────────────────
            var textColor = ObtenerColorTexto(estado);

            // Prefijo de estado
            string prefijo = estado switch
            {
                EstadoAgente.Done  => "✓",
                EstadoAgente.Error => "✕",
                _                  => Pasos[idx]
            };

            // Font: peso según estado
            FontStyle fStyle = estado == EstadoAgente.Active ? FontStyle.Bold : FontStyle.Regular;

            // Área del número (estrecha, izquierda)
            int numW = r.Height + 2; // cuadrado aprox
            Rectangle rNum  = new Rectangle(r.X + 4, r.Y, numW, r.Height);
            Rectangle rName = new Rectangle(r.X + numW + 6, r.Y, r.Width - numW - 10, r.Height);

            var sfCenter = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.None
            };
            var sfLeft = new StringFormat
            {
                Alignment     = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.EllipsisCharacter
            };

            // Número / símbolo de estado
            using var fontNum  = new Font("Segoe UI", 7f, FontStyle.Bold);
            using var fontName = new Font("Segoe UI", 7f, fStyle);
            using var brush    = new SolidBrush(textColor);

            // Color del número más destacado en activo
            Color colorNum = estado == EstadoAgente.Idle
                ? IdleText
                : Color.FromArgb(220, textColor);

            using var brushNum = new SolidBrush(colorNum);
            g.DrawString(prefijo, fontNum, brushNum, rNum, sfCenter);
            g.DrawString(Nombres[idx], fontName, brush, rName, sfLeft);
        }

        private Color ObtenerColorBase(int idx, EstadoAgente estado) => estado switch
        {
            EstadoAgente.Active => ActiveColors[idx],
            EstadoAgente.Done   => DoneColor,
            EstadoAgente.Error  => ErrorColor,
            _                   => IdleColor
        };

        private static Color ObtenerColorTexto(EstadoAgente estado) => estado switch
        {
            EstadoAgente.Done  => TextDone,
            EstadoAgente.Error => TextError,
            EstadoAgente.Idle  => IdleText,
            _                  => TextActive
        };

        private static GraphicsPath CrearRoundedRect(Rectangle r, int radio)
        {
            int d = radio * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _pulseTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
