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
        private static readonly Color BgColor        = Color.FromArgb(18, 24, 38);
        private static readonly Color IdleColor      = Color.FromArgb(55, 65, 85);
        private static readonly Color IdleText       = Color.FromArgb(130, 140, 160);

        // Colores activos por agente
        private static readonly Color[] ActiveColors =
        {
            Color.FromArgb(59, 130, 246),   // Analista    → azul
            Color.FromArgb(16, 185, 129),   // Constructor → verde esmeralda
            Color.FromArgb(245, 158, 11),   // Guardián    → ámbar
            Color.FromArgb(139, 92, 246),   // Comunicador → violeta
        };

        private static readonly Color DoneColor   = Color.FromArgb(34, 197, 94);
        private static readonly Color ErrorColor  = Color.FromArgb(239, 68, 68);
        private static readonly Color TextActive  = Color.FromArgb(240, 240, 250);
        private static readonly Color TextDone    = Color.FromArgb(220, 252, 231);
        private static readonly Color TextError   = Color.FromArgb(254, 202, 202);

        // ── Iconos por agente ─────────────────────────────────────────────────
        private static readonly string[] Iconos  = { "🔍", "⚙", "🛡", "💬" };
        private static readonly string[] Nombres = { "ANALISTA", "CONSTRUCTOR", "GUARDIÁN", "COMUNICADOR" };

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
            Height         = 40;
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

            int totalAgentes = 4;
            int padding      = 8;
            int gap          = 6;
            int pillH        = Height - padding * 2;
            int totalW       = Width - padding * 2;
            int pillW        = (totalW - gap * (totalAgentes - 1)) / totalAgentes;

            for (int i = 0; i < totalAgentes; i++)
            {
                int x = padding + i * (pillW + gap);
                int y = padding;
                DibujarPildora(g, i, new Rectangle(x, y, pillW, pillH));
            }
        }

        private void DibujarPildora(Graphics g, int idx, Rectangle r)
        {
            var estado = _estados[idx];
            var baseColor = ObtenerColorBase(idx, estado);

            // Pulso: si está activo, variar la opacidad para efecto pulsante
            if (estado == EstadoAgente.Active)
            {
                double t    = _pulseTick / 60.0;
                double alfa = 0.65 + 0.35 * Math.Sin(t * Math.PI * 2);
                baseColor   = Color.FromArgb((int)(255 * alfa), baseColor);
            }

            // Fondo de la píldora con radio = pillH/2
            int radio = r.Height / 2;
            using var path = CrearRoundedRect(r, radio);
            using var fill = new SolidBrush(baseColor);
            g.FillPath(fill, path);

            // Borde sutil
            using var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f);
            g.DrawPath(pen, path);

            // Icono + nombre
            var textColor = ObtenerColorTexto(estado);
            string label  = $"{Iconos[idx]}  {Nombres[idx]}";

            // Checkmark si completado
            if (estado == EstadoAgente.Done)   label = $"✓  {Nombres[idx]}";
            if (estado == EstadoAgente.Error)  label = $"✕  {Nombres[idx]}";

            using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var textBrush = new SolidBrush(textColor);

            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming      = StringTrimming.EllipsisCharacter
            };

            g.DrawString(label, font, textBrush, r, sf);
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
