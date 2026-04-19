// ============================================================
//  MemoriaManager.cs  — Fase 1 del sistema de Memoria
//
//  QUÉ HACE:
//    Lee y escribe dos archivos de memoria durable asociados a
//    cada ruta de trabajo:
//      - Hechos.md      → verdades sobre el usuario / su entorno
//      - Episodios.md   → timeline append-only de ejecuciones
//
//  DISEÑO:
//    Cero BD. Archivos markdown editables a mano. La memoria
//    "viaja" con la carpeta de trabajo.
//    En esta Fase 1 la memoria es estática: el usuario la edita
//    en FrmMemoria y se inyecta en el prompt del agente.
//    La Fase 2 (Memorista automático) se engancha arriba de esto
//    sin refactor.
//
//  PRESUPUESTO DE TOKENS:
//    El método FormatearParaPrompt() recorta a un tope configurable
//    (por defecto ~800 tokens ≈ 3200 caracteres). Los hechos
//    se conservan enteros; los episodios se truncan desde los
//    más viejos.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Utilerias
{
    public static class MemoriaManager
    {
        // Aproximación tosca pero suficiente: 1 token ≈ 4 caracteres.
        // Mantenemos el presupuesto en caracteres para no depender de
        // tokenizers específicos por proveedor.
        private const int PresupuestoCaracteres = 3200;

        // ── Lectura cruda ────────────────────────────────────────────

        public static async Task<string> LeerHechosAsync(string rutaBase)
        {
            if (string.IsNullOrWhiteSpace(rutaBase)) return "";
            string ruta = RutasProyecto.ObtenerRutaMemoriaHechos(rutaBase);
            if (!File.Exists(ruta)) return "";
            return await File.ReadAllTextAsync(ruta);
        }

        public static async Task<string> LeerEpisodiosAsync(string rutaBase)
        {
            if (string.IsNullOrWhiteSpace(rutaBase)) return "";
            string ruta = RutasProyecto.ObtenerRutaMemoriaEpisodios(rutaBase);
            if (!File.Exists(ruta)) return "";
            return await File.ReadAllTextAsync(ruta);
        }

        // ── Escritura ────────────────────────────────────────────────

        public static async Task GuardarHechosAsync(string rutaBase, string contenido)
        {
            if (string.IsNullOrWhiteSpace(rutaBase)) return;
            string ruta = RutasProyecto.ObtenerRutaMemoriaHechos(rutaBase);
            await File.WriteAllTextAsync(ruta, contenido ?? "");
        }

        public static async Task GuardarEpisodiosAsync(string rutaBase, string contenido)
        {
            if (string.IsNullOrWhiteSpace(rutaBase)) return;
            string ruta = RutasProyecto.ObtenerRutaMemoriaEpisodios(rutaBase);
            await File.WriteAllTextAsync(ruta, contenido ?? "");
        }

        /// <summary>
        /// Agrega un episodio al final de Episodios.md con fecha/hora ISO.
        /// Pensado para la Fase 2 (Memorista automático), pero utilizable
        /// desde ya para registro manual.
        /// </summary>
        public static async Task AgregarEpisodioAsync(string rutaBase, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(rutaBase)) return;
            if (string.IsNullOrWhiteSpace(descripcion)) return;

            string ruta = RutasProyecto.ObtenerRutaMemoriaEpisodios(rutaBase);
            string linea = $"- {DateTime.Now:yyyy-MM-dd HH:mm} — {descripcion.Trim()}{Environment.NewLine}";

            // Crear archivo con encabezado si no existe
            if (!File.Exists(ruta))
            {
                string encabezado =
                    "# Episodios" + Environment.NewLine +
                    "Timeline de ejecuciones relevantes (append-only)." + Environment.NewLine +
                    Environment.NewLine;
                await File.WriteAllTextAsync(ruta, encabezado);
            }

            await File.AppendAllTextAsync(ruta, linea);
        }

        // ── Formato para inyección en prompt ─────────────────────────

        /// <summary>
        /// Construye la sección ================= MEMORIA =================
        /// que se añade al prompt efectivo. Si no hay memoria, devuelve
        /// string vacío (el AgentContext omite la sección limpiamente).
        /// </summary>
        public static async Task<string> FormatearParaPromptAsync(string rutaBase)
        {
            if (string.IsNullOrWhiteSpace(rutaBase)) return "";

            string hechos = await LeerHechosAsync(rutaBase);
            string episodios = await LeerEpisodiosAsync(rutaBase);

            hechos = LimpiarMarkdown(hechos);
            episodios = LimpiarMarkdown(episodios);

            if (string.IsNullOrWhiteSpace(hechos) && string.IsNullOrWhiteSpace(episodios))
                return "";

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================= MEMORIA =================");
            sb.AppendLine("Contexto persistente acumulado sobre el usuario y sus tareas.");
            sb.AppendLine("Úsalo para personalizar tus respuestas sin pedir que te lo repita.");

            if (!string.IsNullOrWhiteSpace(hechos))
            {
                sb.AppendLine();
                sb.AppendLine("-- Hechos --");
                sb.AppendLine(hechos.Trim());
            }

            if (!string.IsNullOrWhiteSpace(episodios))
            {
                sb.AppendLine();
                sb.AppendLine("-- Episodios recientes --");
                sb.AppendLine(RecortarEpisodios(episodios, PresupuestoCaracteres - hechos.Length - 200));
            }

            sb.AppendLine();
            return sb.ToString();
        }

        // ── Helpers internos ─────────────────────────────────────────

        /// <summary>
        /// Elimina encabezados markdown (#, ##) y líneas vacías extra.
        /// No tocamos el resto para no romper viñetas que el LLM sabe leer.
        /// </summary>
        private static string LimpiarMarkdown(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";

            var lineas = texto
                .Replace("\r\n", "\n")
                .Split('\n')
                .Where(l => !l.TrimStart().StartsWith("#"))
                .ToList();

            // Colapsar líneas vacías consecutivas
            var resultado = new List<string>();
            bool ultimaVacia = false;
            foreach (var l in lineas)
            {
                bool vacia = string.IsNullOrWhiteSpace(l);
                if (vacia && ultimaVacia) continue;
                resultado.Add(l);
                ultimaVacia = vacia;
            }

            return string.Join("\n", resultado).Trim();
        }

        /// <summary>
        /// Si los episodios exceden el presupuesto, se quedan los más recientes
        /// (últimas líneas del archivo, que es append-only → más abajo = más nuevo).
        /// </summary>
        private static string RecortarEpisodios(string episodios, int presupuesto)
        {
            if (presupuesto <= 200) presupuesto = 200;
            if (episodios.Length <= presupuesto) return episodios;

            var lineas = episodios.Split('\n').ToList();
            var mantenidas = new List<string>();
            int acum = 0;

            for (int i = lineas.Count - 1; i >= 0; i--)
            {
                int costo = lineas[i].Length + 1;
                if (acum + costo > presupuesto) break;
                mantenidas.Insert(0, lineas[i]);
                acum += costo;
            }

            if (mantenidas.Count < lineas.Count)
                mantenidas.Insert(0, "(… episodios más antiguos recortados …)");

            return string.Join("\n", mantenidas);
        }

        // ── Metrica UI ───────────────────────────────────────────────

        /// <summary>
        /// Para mostrar en el footer del FrmMemoria: "Memoria activa: ~412 tokens".
        /// </summary>
        public static async Task<int> EstimarTokensActivosAsync(string rutaBase)
        {
            string formato = await FormatearParaPromptAsync(rutaBase);
            if (string.IsNullOrEmpty(formato)) return 0;
            return Math.Max(1, formato.Length / 4);
        }
    }
}
