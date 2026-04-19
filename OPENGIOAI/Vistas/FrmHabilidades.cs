// ============================================================
//  FrmHabilidades.cs
//
//  Módulo donde el usuario activa/desactiva las capacidades
//  cognitivas del agente (memoria, patrones, sugerencias...).
//
//  DIFERENCIA CON Skills.cs:
//    - Skills.cs gestiona plugins externos en Python (QUÉ hace el
//      agente).
//    - FrmHabilidades gestiona toggles internos de comportamiento
//      del orquestador (CÓMO procesa el agente).
//
//  FLUJO:
//    1. Al cargarse el form: lee HabilidadesRegistry → renderiza
//       tarjetas en pnlLista con posicionamiento manual.
//    2. Usuario pulsa el switch de una tarjeta → Registry.Establecer
//       persiste inmediatamente en ListHabilidades.json.
//    3. La próxima ejecución del pipeline consulta Registry.EstaActiva
//       y actúa en consecuencia.
//
//  LAYOUT:
//    Usamos un Panel plano con posicionamiento manual (no FlowLayoutPanel)
//    porque este form vive embebido en pnlContenedor de FrmPrincipal vía
//    EmeraldTheme.OpenOrShowFormInPanel(...) — el tamaño real solo se
//    conoce tras el Load, así que pintamos ahí y re-pintamos en Resize.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmHabilidades : Form
    {
        public FrmHabilidades()
        {
            InitializeComponent();
            AplicarTema();

            // Pintamos tras el Load (cuando el form ya tiene su tamaño real
            // dentro del pnlContenedor) y re-pintamos si cambia el tamaño.
            this.Load   += (s, e) => CargarHabilidades();
            pnlLista.Resize += (s, e) => CargarHabilidades();
        }

        private void AplicarTema()
        {
            BackColor = EmeraldTheme.BgDeep;
            ForeColor = EmeraldTheme.TextPrimary;

            pnlRoot.BackColor   = EmeraldTheme.BgDeep;
            pnlHeader.BackColor = EmeraldTheme.BgDeep;
            pnlLista.BackColor  = EmeraldTheme.BgDeep;

            lblTitulo.ForeColor    = EmeraldTheme.Emerald400;
            lblSubtitulo.ForeColor = EmeraldTheme.TextMuted;
        }

        // ══════════════════ Render ══════════════════

        private void CargarHabilidades()
        {
            pnlLista.SuspendLayout();
            pnlLista.Controls.Clear();

            var habilidades = HabilidadesRegistry.Instancia.Todas();

            // Ancho útil: ClientSize menos padding horizontal (8 + 8 = 16) y
            // margen interno (12 a cada lado). Si aún no hay ClientSize real
            // fallback a 820 como mínimo razonable.
            int anchoUtil = Math.Max(pnlLista.ClientSize.Width - 32, 820);

            int y = 12;
            const int altoTarjeta = 120;
            const int gap = 12;

            foreach (var h in habilidades)
            {
                var tarjeta = ConstruirTarjeta(h, anchoUtil, altoTarjeta);
                tarjeta.Location = new Point(12, y);
                tarjeta.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                pnlLista.Controls.Add(tarjeta);
                y += altoTarjeta + gap;
            }

            if (habilidades.Count == 0)
            {
                var vacio = new Label
                {
                    Text = "No hay habilidades registradas.",
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = EmeraldTheme.TextMuted,
                    AutoSize = true,
                    Location = new Point(20, 20),
                };
                pnlLista.Controls.Add(vacio);
            }

            pnlLista.ResumeLayout();
        }

        private Panel ConstruirTarjeta(Habilidad h, int ancho, int alto)
        {
            var card = new Panel
            {
                Size = new Size(ancho, alto),
                BackColor = EmeraldTheme.BgCard,
                Tag = h,
            };

            // Borde sutil: emerald si activa, gris si no
            card.Paint += (s, e) =>
            {
                Color borde = h.Activa ? EmeraldTheme.Emerald500 : Color.FromArgb(40, 40, 40);
                using var pen = new Pen(borde, 1f);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            // Icono grande
            var lblIcono = new Label
            {
                Text = string.IsNullOrEmpty(h.Icono) ? "⚙" : h.Icono,
                Font = new Font("Segoe UI Emoji", 22F),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(18, 20),
            };

            // Título
            var lblNombre = new Label
            {
                Text = h.Nombre,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(76, 20),
            };

            // Descripción (ancha, usa el espacio restante)
            var lblDesc = new Label
            {
                Text = h.Descripcion,
                Font = new Font("Segoe UI", 9F),
                ForeColor = EmeraldTheme.TextSecondary,
                Location = new Point(76, 46),
                Size = new Size(ancho - 76 - 170, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            // Impacto de tokens
            var lblImpacto = new Label
            {
                Text = string.IsNullOrEmpty(h.ImpactoTokens) ? "" : $"⚡ {h.ImpactoTokens}",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = h.Activa ? EmeraldTheme.Emerald400 : EmeraldTheme.TextMuted,
                AutoSize = true,
                Location = new Point(76, 92),
            };

            // Switch — anclado a la derecha
            var btnSwitch = new Button
            {
                Text = h.Activa ? "🟢  ACTIVA" : "⚪  INACTIVA",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(140, 40),
                Location = new Point(ancho - 160, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                BackColor = h.Activa ? EmeraldTheme.Emerald900 : EmeraldTheme.BgSurface,
                ForeColor = h.Activa ? EmeraldTheme.TextPrimary : EmeraldTheme.TextMuted,
            };
            btnSwitch.FlatAppearance.BorderColor = h.Activa
                ? EmeraldTheme.Emerald400
                : Color.FromArgb(60, 60, 60);
            btnSwitch.FlatAppearance.BorderSize = 1;
            btnSwitch.FlatAppearance.MouseOverBackColor = EmeraldTheme.Emerald600;

            btnSwitch.Click += (s, e) =>
            {
                bool nuevoEstado = !h.Activa;
                HabilidadesRegistry.Instancia.Establecer(h.Clave, nuevoEstado);
                CargarHabilidades(); // re-render con el nuevo estado visual
            };

            card.Controls.Add(lblIcono);
            card.Controls.Add(lblNombre);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblImpacto);
            card.Controls.Add(btnSwitch);

            return card;
        }
    }
}
