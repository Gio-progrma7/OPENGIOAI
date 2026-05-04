using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace OPENGIOAI.Utilerias
{
    /// <summary>
    /// Genera el archivo OPENGIOAI.ico en múltiples resoluciones (16/32/48/256 px)
    /// usando GDI+. Llama a <see cref="GenerarSiNoExiste"/> al iniciar la app.
    /// </summary>
    public static class GeneradorIcono
    {
        // ── Paleta del ícono ─────────────────────────────────────────────────
        private static readonly Color BgOscuro  = Color.FromArgb(255, 20,  20,  24);
        private static readonly Color BgTarjeta = Color.FromArgb(255, 28,  30,  38);
        private static readonly Color Esmeralda = Color.FromArgb(255, 148, 230, 236); // #94E6EC
        private static readonly Color Acento2   = Color.FromArgb(255,  65,  88, 208); // #4158D0

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>
        /// Genera el ícono solo si aún no existe en la ruta indicada.
        /// Llama esto desde Program.cs antes de Application.Run().
        /// </summary>
        public static void GenerarSiNoExiste(string rutaIco)
        {
            if (!File.Exists(rutaIco))
                Generar(rutaIco);
        }

        /// <summary>Fuerza la regeneración del ícono.</summary>
        public static void Generar(string rutaIco)
        {
            int[] tamaños = { 16, 32, 48, 256 };
            var pngs = new byte[tamaños.Length][];

            for (int i = 0; i < tamaños.Length; i++)
            {
                using var bmp = DibujarIcono(tamaños[i]);
                using var ms  = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                pngs[i] = ms.ToArray();
            }

            using var salida = new MemoryStream();
            using var bw     = new BinaryWriter(salida);

            // ── ICONDIR header ──
            bw.Write((short)0);                  // reserved
            bw.Write((short)1);                  // type: 1 = icon
            bw.Write((short)tamaños.Length);     // número de imágenes

            // ── ICONDIRENTRY para cada tamaño ──
            // offset: 6 (header) + count * 16 (entries)
            int offset = 6 + tamaños.Length * 16;
            for (int i = 0; i < tamaños.Length; i++)
            {
                int sz = tamaños[i];
                bw.Write((byte)(sz == 256 ? 0 : sz)); // width  (0 = 256)
                bw.Write((byte)(sz == 256 ? 0 : sz)); // height (0 = 256)
                bw.Write((byte)0);    // colorCount (0 = sin paleta)
                bw.Write((byte)0);    // reserved
                bw.Write((short)1);   // planes
                bw.Write((short)32);  // bitCount
                bw.Write(pngs[i].Length);
                bw.Write(offset);
                offset += pngs[i].Length;
            }

            // ── Datos PNG de cada imagen ──
            foreach (var png in pngs)
                bw.Write(png);

            string? dir = Path.GetDirectoryName(rutaIco);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllBytes(rutaIco, salida.ToArray());
        }

        // ── Diseño del ícono ─────────────────────────────────────────────────

        private static Bitmap DibujarIcono(int sz)
        {
            var bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);

            g.SmoothingMode        = SmoothingMode.AntiAlias;
            g.InterpolationMode    = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint    = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            float pad    = Math.Max(1f, sz * 0.04f);
            float radio  = sz * 0.22f;
            var   rc     = new RectangleF(pad, pad, sz - pad * 2, sz - pad * 2);

            // ── Fondo con gradiente ──
            using var bgPath = RoundedRect(rc, radio);
            using var bgBrush = new LinearGradientBrush(
                new PointF(0, 0), new PointF(sz, sz),
                BgOscuro, BgTarjeta);
            g.FillPath(bgBrush, bgPath);

            // ── Borde de acento ──
            float grosorBorde = Math.Max(1f, sz * 0.04f);
            using var borderPen = new Pen(Color.FromArgb(160, Esmeralda), grosorBorde);
            g.DrawPath(borderPen, bgPath);

            // ── Símbolo central ──
            if (sz >= 48)
            {
                DibujarEstrellaConPunto(g, sz);
            }
            else if (sz >= 24)
            {
                // Estrella simplificada
                float r = sz * 0.28f;
                float cx = sz / 2f, cy = sz / 2f;
                using var b1 = new SolidBrush(Esmeralda);
                g.FillEllipse(b1, cx - r, cy - r, r * 2, r * 2);

                float ri = r * 0.45f;
                using var b2 = new SolidBrush(BgOscuro);
                g.FillEllipse(b2, cx - ri, cy - ri, ri * 2, ri * 2);
            }
            else
            {
                // 16×16: bloque sólido esmeralda
                float r = sz * 0.3f;
                float cx = sz / 2f, cy = sz / 2f;
                using var b = new SolidBrush(Esmeralda);
                g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
            }

            return bmp;
        }

        /// <summary>
        /// Dibuja el símbolo ✦ (estrella de 4 puntas con punto central)
        /// usando líneas GDI+ — no depende de la fuente.
        /// </summary>
        private static void DibujarEstrellaConPunto(Graphics g, int sz)
        {
            float cx  = sz / 2f;
            float cy  = sz / 2f;
            float ext = sz * 0.36f;   // largo de cada punta
            float ancho = sz * 0.08f; // semiancho en la base de la punta

            // 4 puntas: arriba, abajo, izquierda, derecha
            PointF[][] puntas =
            {
                Diamond(cx, cy, 0,    -ext, ancho),  // arriba
                Diamond(cx, cy, 0,    +ext, ancho),  // abajo
                Diamond(cx, cy, -ext, 0,    ancho),  // izquierda
                Diamond(cx, cy, +ext, 0,    ancho),  // derecha
            };

            // Relleno con gradiente esmeralda → azul
            using var brush = new LinearGradientBrush(
                new PointF(cx - ext, cy - ext),
                new PointF(cx + ext, cy + ext),
                Esmeralda, Acento2);

            foreach (var punta in puntas)
                g.FillPolygon(brush, punta);

            // Punto brillante central
            float rc2 = sz * 0.07f;
            using var bCenter = new SolidBrush(Color.White);
            g.FillEllipse(bCenter, cx - rc2, cy - rc2, rc2 * 2, rc2 * 2);
        }

        /// <summary>Devuelve un diamante (rombo) de 4 puntos apuntando en la dirección dx,dy.</summary>
        private static PointF[] Diamond(float cx, float cy, float dx, float dy, float ancho)
        {
            // Perpendicular normalizada
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len == 0) return Array.Empty<PointF>();
            float px = -dy / len * ancho;
            float py =  dx / len * ancho;

            return new[]
            {
                new PointF(cx,         cy),          // centro (base)
                new PointF(cx + px,    cy + py),     // lado izq
                new PointF(cx + dx,    cy + dy),     // punta
                new PointF(cx - px,    cy - py),     // lado der
            };
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d    = radius * 2f;
            var   path = new GraphicsPath();
            path.AddArc(r.X,          r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d,  r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d,  r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,          r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
