using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Themas
{
    public class DatoGrafica
    {
        public string Etiqueta    { get; set; } = "";
        public double Valor       { get; set; }
        public Color? ColorOverride { get; set; }
    }

    /// <summary>
    /// Gráfica de barras verticales con GDI+. Usa la paleta de EmeraldTheme.
    /// Actualiza llamando SetDatos() — redibuja automáticamente.
    /// </summary>
    public class GraficaBarras : Control
    {
        private List<DatoGrafica> _datos  = new();
        private int _hoverIdx             = -1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Titulo        { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FormatoValor  { get; set; } = "{0:N0}";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color  ColorDefault  { get; set; } = ColorTranslator.FromHtml("#94E6EC");

        // Padding interno constante
        private const int PL = 10, PR = 10, PT = 32, PB = 34, ValGap = 16;

        public GraficaBarras()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint, true);
        }

        public void SetDatos(List<DatoGrafica> datos)
        {
            _datos    = datos ?? new List<DatoGrafica>();
            _hoverIdx = -1;
            Invalidate();
        }

        // ── Paint ─────────────────────────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode         = SmoothingMode.AntiAlias;
            g.TextRenderingHint     = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode     = InterpolationMode.High;

            var rc = ClientRectangle;
            g.Clear(EmeraldTheme.BgCard);

            // ── Título ──
            using var ftTitulo = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
            using var brTitle  = new SolidBrush(EmeraldTheme.TextPrimary);
            if (!string.IsNullOrEmpty(Titulo))
                g.DrawString(Titulo, ftTitulo, brTitle,
                    new RectangleF(PL, 6, rc.Width - PL - PR, 22),
                    new StringFormat { Alignment = StringAlignment.Near,
                                       LineAlignment = StringAlignment.Center });

            if (_datos.Count == 0)
            {
                using var ftEmpty = new Font("Segoe UI", 8.5f);
                using var brEmpty = new SolidBrush(EmeraldTheme.TextMuted);
                g.DrawString("Sin datos aún", ftEmpty, brEmpty, rc,
                    new StringFormat { Alignment = StringAlignment.Center,
                                       LineAlignment = StringAlignment.Center });
                return;
            }

            int   chartW = rc.Width  - PL - PR;
            int   chartH = rc.Height - PT  - PB;
            if (chartW <= 0 || chartH <= 0) return;

            double maxVal = _datos.Max(d => d.Valor);
            if (maxVal <= 0) maxVal = 1;

            int   n    = _datos.Count;
            float gap  = Math.Max(2f, chartW * 0.06f / Math.Max(1, n));
            float barW = Math.Max(4f, (chartW - gap * (n + 1)) / n);

            using var ftLabel = new Font("Segoe UI", 7.2f);
            using var ftVal   = new Font("Segoe UI Semibold", 7.4f, FontStyle.Bold);
            var sfCenter = new StringFormat { Alignment = StringAlignment.Center,
                                              LineAlignment = StringAlignment.Far };
            var sfCenterTop = new StringFormat { Alignment = StringAlignment.Center,
                                                 LineAlignment = StringAlignment.Near };

            for (int i = 0; i < n; i++)
            {
                var   d       = _datos[i];
                float x       = PL + gap * (i + 1) + barW * i;
                float barH    = (float)(d.Valor / maxVal * (chartH - ValGap));
                if (barH < 1) barH = 1;
                float y = PT + (chartH - barH - ValGap);

                Color col     = d.ColorOverride ?? ColorDefault;
                bool  hovered = i == _hoverIdx;

                if (hovered)
                    col = Color.FromArgb(
                        Math.Min(255, col.R + 40),
                        Math.Min(255, col.G + 40),
                        Math.Min(255, col.B + 40));

                // ── Barra con esquinas superiores redondeadas ──
                using var path  = RoundedTop(new RectangleF(x, y, barW, barH), 3);
                using var brush = new SolidBrush(Color.FromArgb(hovered ? 230 : 185, col));
                g.FillPath(brush, path);
                using var pen = new Pen(Color.FromArgb(hovered ? 255 : 215, col), 1f);
                g.DrawPath(pen, path);

                // ── Valor encima ──
                if (barH >= 12)
                {
                    string valStr = string.Format(FormatoValor, d.Valor);
                    using var brVal = new SolidBrush(EmeraldTheme.TextPrimary);
                    g.DrawString(valStr, ftVal, brVal,
                        new RectangleF(x - 12, y - ValGap + 2, barW + 24, ValGap - 2), sfCenter);
                }

                // ── Etiqueta debajo ──
                string lbl = d.Etiqueta.Length > 10 ? d.Etiqueta[..9] + "…" : d.Etiqueta;
                using var brLabel = new SolidBrush(hovered ? EmeraldTheme.TextPrimary : EmeraldTheme.TextMuted);
                g.DrawString(lbl, ftLabel, brLabel,
                    new RectangleF(x - gap / 2f, rc.Height - PB, barW + gap, PB), sfCenterTop);
            }

            // ── Línea base ──
            int baseY = PT + chartH - ValGap;
            using var brBase = new SolidBrush(EmeraldTheme.IsDark
                ? Color.FromArgb(40, 255, 255, 255)
                : Color.FromArgb(40, 0, 0, 0));
            g.FillRectangle(brBase, new RectangleF(PL, baseY, chartW, 1));
        }

        private static GraphicsPath RoundedTop(RectangleF r, float radius)
        {
            float d    = radius * 2f;
            var   path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
            return path;
        }

        // ── Hover ────────────────────────────────────────────────────────────

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hit = HitTest(e.X);
            if (hit != _hoverIdx) { _hoverIdx = hit; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverIdx != -1) { _hoverIdx = -1; Invalidate(); }
        }

        private int HitTest(int mx)
        {
            if (_datos.Count == 0) return -1;
            int   chartW = Width - PL - PR;
            if (chartW <= 0) return -1;
            int   n   = _datos.Count;
            float gap = Math.Max(2f, chartW * 0.06f / Math.Max(1, n));
            float barW = Math.Max(4f, (chartW - gap * (n + 1)) / n);
            for (int i = 0; i < n; i++)
            {
                float x = PL + gap * (i + 1) + barW * i;
                if (mx >= x - 3 && mx <= x + barW + 3) return i;
            }
            return -1;
        }
    }
}
