// ============================================================
//  FrmPatrones.cs  — Fase 3 del sistema de Memoria
//
//  UI donde el usuario ve los patrones recurrentes que el agente
//  ha detectado en Episodios.md y decide:
//     · Convertir en Skill → genera un .md en {ruta}/skills/
//     · Ignorar             → añade la firma a PatronesIgnorados.json
//
//  GATES:
//    - Si no hay ruta de trabajo activa → mensaje.
//    - Si la habilidad "patrones" está desactivada → mensaje con
//      enlace (implícito) a FrmHabilidades.
//
//  FLUJO:
//    1. Usuario entra al form — NO se dispara el LLM automáticamente.
//    2. Pulsa "Analizar ahora" → AnalizadorPatrones ejecuta detección
//       local + enriquecimiento con LLM (1 llamada por cluster).
//    3. Se pintan tarjetas con la sugerencia.
//    4. Botones por tarjeta: Convertir / Ignorar — ambos persisten.
//
//  LAYOUT:
//    Panel plano con posicionamiento manual (mismo patrón que
//    FrmHabilidades). Re-pinta en Resize.
// ============================================================

using OPENGIOAI.Agentes;
using OPENGIOAI.Data;
using OPENGIOAI.Entidades;
using OPENGIOAI.Skills;
using OPENGIOAI.Themas;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmPatrones : Form
    {
        private readonly string _rutaTrabajo;
        private readonly ConfiguracionClient _config;
        private List<PatronDetectado> _patrones = new();
        private CancellationTokenSource? _cts;

        public FrmPatrones(string rutaTrabajo, ConfiguracionClient config)
        {
            InitializeComponent();
            _rutaTrabajo = rutaTrabajo ?? "";
            _config      = config ?? new ConfiguracionClient();

            AplicarTema();
            btnAnalizar.Click += async (s, e) => await AnalizarAsync();
            this.Load   += (s, e) => PintarEstadoInicial();
            pnlLista.Resize += (s, e) => RenderLista();
            this.FormClosing += (s, e) => _cts?.Cancel();
        }

        // ══════════════════ Tema ══════════════════

        private void AplicarTema()
        {
            BackColor = EmeraldTheme.BgDeep;
            ForeColor = EmeraldTheme.TextPrimary;

            pnlRoot.BackColor     = EmeraldTheme.BgDeep;
            pnlHeader.BackColor   = EmeraldTheme.BgDeep;
            pnlAcciones.BackColor = EmeraldTheme.BgDeep;
            pnlLista.BackColor    = EmeraldTheme.BgDeep;

            lblTitulo.ForeColor    = EmeraldTheme.Emerald400;
            lblSubtitulo.ForeColor = EmeraldTheme.TextMuted;
            lblEstado.ForeColor    = EmeraldTheme.TextMuted;

            btnAnalizar.BackColor = EmeraldTheme.Emerald900;
            btnAnalizar.ForeColor = EmeraldTheme.TextPrimary;
            btnAnalizar.FlatAppearance.BorderColor = EmeraldTheme.Emerald400;
            btnAnalizar.FlatAppearance.BorderSize  = 1;
            btnAnalizar.FlatAppearance.MouseOverBackColor = EmeraldTheme.Emerald600;
        }

        // ══════════════════ Estados ══════════════════

        private void PintarEstadoInicial()
        {
            if (string.IsNullOrWhiteSpace(_rutaTrabajo))
            {
                MostrarMensaje(
                    "No hay ruta de trabajo activa.",
                    "Selecciona una ruta desde Mandos para poder analizar patrones.");
                btnAnalizar.Enabled = false;
                return;
            }

            if (!HabilidadesRegistry.Instancia.EstaActiva(HabilidadesRegistry.HAB_PATRONES))
            {
                MostrarMensaje(
                    "La habilidad «Detección de patrones» está desactivada.",
                    "Actívala desde el módulo ⚙ Habilidades para poder buscar patrones en tu historial.");
                btnAnalizar.Enabled = false;
                return;
            }

            MostrarMensaje(
                "Listo para analizar tu historial.",
                "Pulsa «Analizar ahora» para buscar tareas recurrentes en Episodios.md.");
            btnAnalizar.Enabled = true;
        }

        private void MostrarMensaje(string titulo, string subtitulo)
        {
            pnlLista.SuspendLayout();
            pnlLista.Controls.Clear();

            var lblT = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20),
            };
            var lblS = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = EmeraldTheme.TextMuted,
                AutoSize = true,
                Location = new Point(20, 50),
            };
            pnlLista.Controls.Add(lblT);
            pnlLista.Controls.Add(lblS);
            pnlLista.ResumeLayout();
        }

        // ══════════════════ Análisis ══════════════════

        private async Task AnalizarAsync()
        {
            try
            {
                btnAnalizar.Enabled = false;
                lblEstado.Text = "Analizando…";
                pnlLista.Controls.Clear();

                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                var ctx = await ConstruirContextoAsync(_cts.Token);

                _patrones = await AnalizadorPatrones.AnalizarAsync(
                    _rutaTrabajo, ctx, _cts.Token);

                if (_patrones.Count == 0)
                {
                    MostrarMensaje(
                        "Todavía no hay patrones recurrentes.",
                        "Necesitas al menos 3 episodios similares en Episodios.md para que se detecte un patrón.");
                }
                else
                {
                    lblEstado.Text = $"{_patrones.Count} patrón(es) detectado(s).";
                    RenderLista();
                }
            }
            catch (OperationCanceledException)
            {
                lblEstado.Text = "Análisis cancelado.";
            }
            catch (Exception ex)
            {
                lblEstado.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnAnalizar.Enabled = true;
            }
        }

        private async Task<AgentContext?> ConstruirContextoAsync(CancellationToken ct)
        {
            try
            {
                string modelo   = _config?.Mimodelo?.Modelos ?? "";
                string apiKey   = _config?.Mimodelo?.ApiKey  ?? "";
                Servicios srv   = _config?.Mimodelo?.Agente ?? Servicios.Ollama;

                // El Analizador de Patrones hace análisis puro de clusters textuales;
                // no necesita credenciales, skills ni memoria. Perfil Mínimo.
                return await AgentContext.BuildAsync(
                    _rutaTrabajo, modelo, apiKey, srv,
                    soloChat: true,
                    clavesDisponibles: "",
                    ct,
                    perfil: OPENGIOAI.Entidades.PerfilContexto.Minimo);
            }
            catch
            {
                return null;
            }
        }

        // ══════════════════ Render ══════════════════

        private void RenderLista()
        {
            if (_patrones == null || _patrones.Count == 0) return;

            pnlLista.SuspendLayout();
            pnlLista.Controls.Clear();

            int anchoUtil = Math.Max(pnlLista.ClientSize.Width - 32, 820);
            int y = 12;
            const int gap = 12;

            foreach (var p in _patrones)
            {
                var card = ConstruirTarjeta(p, anchoUtil);
                card.Location = new Point(12, y);
                card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                pnlLista.Controls.Add(card);
                y += card.Height + gap;
            }

            pnlLista.ResumeLayout();
        }

        private Panel ConstruirTarjeta(PatronDetectado p, int ancho)
        {
            int alto = 210;
            var card = new Panel
            {
                Size = new Size(ancho, alto),
                BackColor = EmeraldTheme.BgCard,
                Tag = p,
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(EmeraldTheme.Emerald900, 1f);
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            };

            string titulo = string.IsNullOrWhiteSpace(p.NombreSugerido)
                ? $"Patrón recurrente · {p.Firma}"
                : p.NombreSugerido;

            var lblIcono = new Label
            {
                Text = "🔎",
                Font = new Font("Segoe UI Emoji", 22F),
                ForeColor = EmeraldTheme.Emerald400,
                AutoSize = true,
                Location = new Point(18, 16),
            };

            var lblNombre = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = EmeraldTheme.TextPrimary,
                AutoSize = false,
                Location = new Point(76, 18),
                Size = new Size(ancho - 76 - 320, 22),
            };

            var lblMeta = new Label
            {
                Text = $"⚡ {p.Ocurrencias} ocurrencias  ·  🏷 {p.Categoria}",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = EmeraldTheme.Emerald400,
                AutoSize = true,
                Location = new Point(76, 42),
            };

            var lblDesc = new Label
            {
                Text = string.IsNullOrWhiteSpace(p.Descripcion)
                        ? "(sin descripción sugerida)"
                        : p.Descripcion,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = EmeraldTheme.TextSecondary,
                Location = new Point(76, 66),
                Size = new Size(ancho - 76 - 40, 36),
                AutoEllipsis = true,
            };

            string ejemplos = p.Ejemplos.Count == 0
                ? ""
                : "• " + string.Join(Environment.NewLine + "• ",
                    p.Ejemplos.Select(e => Recortar(e, 120)));

            var lblEjemplos = new Label
            {
                Text = ejemplos,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = EmeraldTheme.TextMuted,
                Location = new Point(76, 106),
                Size = new Size(ancho - 76 - 40, 54),
                AutoEllipsis = true,
            };

            // Botones en la esquina inferior derecha
            var btnConvertir = new Button
            {
                Text = "➕  Convertir en Skill",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(170, 32),
                Location = new Point(ancho - 360, alto - 44),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                BackColor = EmeraldTheme.Emerald900,
                ForeColor = EmeraldTheme.TextPrimary,
            };
            btnConvertir.FlatAppearance.BorderColor = EmeraldTheme.Emerald400;
            btnConvertir.FlatAppearance.BorderSize = 1;
            btnConvertir.FlatAppearance.MouseOverBackColor = EmeraldTheme.Emerald600;
            btnConvertir.Click += (s, e) => ConvertirEnSkill(p, card);

            var btnIgnorar = new Button
            {
                Text = "🚫  Ignorar",
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 32),
                Location = new Point(ancho - 180, alto - 44),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Cursor = Cursors.Hand,
                BackColor = EmeraldTheme.BgSurface,
                ForeColor = EmeraldTheme.TextMuted,
            };
            btnIgnorar.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btnIgnorar.FlatAppearance.BorderSize = 1;
            btnIgnorar.Click += async (s, e) => await IgnorarPatronAsync(p, card);

            card.Controls.Add(lblIcono);
            card.Controls.Add(lblNombre);
            card.Controls.Add(lblMeta);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblEjemplos);
            card.Controls.Add(btnConvertir);
            card.Controls.Add(btnIgnorar);

            return card;
        }

        // ══════════════════ Acciones ══════════════════

        private void ConvertirEnSkill(PatronDetectado p, Panel card)
        {
            try
            {
                string id = GenerarIdSkill(p);
                string md = ConstruirMarkdownSkill(p, id);

                var skill = new Skill
                {
                    Id          = id,
                    Nombre      = string.IsNullOrWhiteSpace(p.NombreSugerido)
                                    ? id : p.NombreSugerido,
                    Descripcion = p.Descripcion,
                    Categoria   = string.IsNullOrWhiteSpace(p.Categoria)
                                    ? "general" : p.Categoria,
                    Ejemplo     = p.EjemploInvocacion,
                    Parametros  = p.ParametrosSugeridos ?? new(),
                    Activa      = false, // el usuario debe revisar el .py antes de activarlo
                };

                SkillLoader.GuardarMd(_rutaTrabajo, skill, md);

                MessageBox.Show(
                    $"Skill «{skill.NombreEfectivo}» creado en:\n{skill.RutaMd}\n\n" +
                    "Edita el bloque ## Código para completar el script Python y " +
                    "activa el skill desde el módulo de Skills cuando esté listo.",
                    "Skill creado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Ignorar automáticamente la firma — ya fue convertida.
                _ = DetectorPatrones.AgregarIgnoradaAsync(_rutaTrabajo, p.Firma);
                RemoverTarjeta(p, card);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo crear el skill:\n" + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task IgnorarPatronAsync(PatronDetectado p, Panel card)
        {
            try
            {
                await DetectorPatrones.AgregarIgnoradaAsync(_rutaTrabajo, p.Firma);
                RemoverTarjeta(p, card);
            }
            catch
            {
                // silencio: la próxima ejecución detectará el patrón de nuevo,
                // el usuario podrá reintentar.
            }
        }

        private void RemoverTarjeta(PatronDetectado p, Panel card)
        {
            _patrones.Remove(p);
            pnlLista.Controls.Remove(card);
            card.Dispose();

            // Re-posicionar las restantes
            RenderLista();

            if (_patrones.Count == 0)
            {
                lblEstado.Text = "Sin patrones pendientes.";
                MostrarMensaje(
                    "Ya procesaste todos los patrones.",
                    "Pulsa «Analizar ahora» de nuevo cuando tu historial crezca.");
            }
            else
            {
                lblEstado.Text = $"{_patrones.Count} patrón(es) pendiente(s).";
            }
        }

        // ══════════════════ Generación del .md ══════════════════

        private string GenerarIdSkill(PatronDetectado p)
        {
            string baseTxt = !string.IsNullOrWhiteSpace(p.NombreSugerido)
                ? p.NombreSugerido
                : p.Firma;

            var sb = new StringBuilder();
            foreach (var c in baseTxt.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else if (c == ' ' || c == '-' || c == '_') sb.Append('_');
            }
            string id = sb.ToString().Trim('_');
            if (string.IsNullOrEmpty(id)) id = "skill_patron";
            if (id.Length > 40) id = id.Substring(0, 40).TrimEnd('_');

            // Evitar colisión con archivos existentes
            string carpeta = Path.Combine(_rutaTrabajo, "skills");
            int intento = 1;
            string candidato = id;
            while (Directory.Exists(carpeta) &&
                   File.Exists(Path.Combine(carpeta, candidato + ".md")))
            {
                intento++;
                candidato = $"{id}_{intento}";
            }
            return candidato;
        }

        private string ConstruirMarkdownSkill(PatronDetectado p, string id)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine($"id: {id}");
            sb.AppendLine($"nombre: {EscapeYaml(string.IsNullOrWhiteSpace(p.NombreSugerido) ? id : p.NombreSugerido)}");
            sb.AppendLine($"categoria: {(string.IsNullOrWhiteSpace(p.Categoria) ? "general" : p.Categoria)}");
            sb.AppendLine($"descripcion: {EscapeYaml(p.Descripcion ?? "")}");
            sb.AppendLine("activa: false");
            if (!string.IsNullOrWhiteSpace(p.EjemploInvocacion))
                sb.AppendLine($"ejemplo: {EscapeYaml(p.EjemploInvocacion)}");
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Descripción");
            sb.AppendLine("Skill generado automáticamente a partir de un patrón recurrente detectado");
            sb.AppendLine($"en tu historial ({p.Ocurrencias} ocurrencias).");
            sb.AppendLine();
            sb.AppendLine("## Ejemplos originales");
            foreach (var ej in p.Ejemplos)
                sb.AppendLine($"- {ej}");
            sb.AppendLine();

            if (p.ParametrosSugeridos != null && p.ParametrosSugeridos.Count > 0)
            {
                sb.AppendLine("## Parámetros");
                foreach (var par in p.ParametrosSugeridos)
                {
                    sb.AppendLine($"- nombre: {par.Nombre} | tipo: {par.Tipo} | " +
                                  $"requerido: {par.Requerido.ToString().ToLowerInvariant()}" +
                                  (string.IsNullOrWhiteSpace(par.Descripcion)
                                      ? ""
                                      : $" | descripcion: {par.Descripcion}"));
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Código");
            sb.AppendLine("```python");
            sb.AppendLine("# TODO: implementa aquí la lógica del skill.");
            sb.AppendLine("# Recuerda escribir el resultado en respuesta.txt");
            sb.AppendLine("# cuando SOLO_CHAT esté activo.");
            sb.AppendLine("def run():");
            sb.AppendLine("    return {\"status\": \"ok\", \"mensaje\": \"skill sin implementar\"}");
            sb.AppendLine();
            sb.AppendLine("if __name__ == \"__main__\":");
            sb.AppendLine("    print(run())");
            sb.AppendLine("```");

            return sb.ToString();
        }

        private static string EscapeYaml(string s)
        {
            if (string.IsNullOrEmpty(s)) return "\"\"";
            // Para el subset simple que usamos, comillas dobles con escape es suficiente.
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
