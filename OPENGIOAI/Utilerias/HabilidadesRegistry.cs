// ============================================================
//  HabilidadesRegistry.cs
//
//  Singleton que gestiona las "habilidades cognitivas" del agente
//  (toggles internos que controlan comportamiento interno, no
//  capacidades externas como los Skills de Python).
//
//  CLAVES CONOCIDAS:
//    · memoria    → activa todo el subsistema de memoria (lectura
//                   del prompt + Memorista automático)
//    · patrones   → activa la detección de patrones sobre Episodios.md
//                   y la propuesta de convertirlos en Skills
//
//  FUENTE DE VERDAD:
//    {AppDir}/ListHabilidades.json — lista serializada de Habilidad.
//    Si una clave conocida no existe en el archivo, se siembra con
//    su default (OFF por defecto — el usuario activa lo que decida).
//
//  RENDIMIENTO:
//    Caché en memoria, invalidado cuando el usuario guarda. La
//    consulta EstaActiva() es O(1) y se llama en hot-path
//    (AgentContext.BuildAsync, AgenteMemorista.EjecutarAsync),
//    por eso NO leemos disco en cada llamada.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using OPENGIOAI.Entidades;

namespace OPENGIOAI.Utilerias
{
    public sealed class HabilidadesRegistry
    {
        private static readonly HabilidadesRegistry _instancia = new();
        public static HabilidadesRegistry Instancia => _instancia;

        private readonly object _lock = new();
        private Dictionary<string, Habilidad> _cache = new(StringComparer.OrdinalIgnoreCase);
        private bool _cargado;

        // ══════════════════ Claves conocidas ══════════════════

        public const string HAB_MEMORIA = "memoria";
        public const string HAB_PATRONES = "patrones";
        public const string HAB_MEMORIA_SEMANTICA = "memoria_semantica";
        public const string HAB_TRACING = "tracing";
        public const string HAB_CONTEXTO_SEMANTICO = "contexto_semantico";
        public const string HAB_HISTORIAL_COMPRIMIDO = "historial_comprimido";

        /// <summary>
        /// Defaults sembrados la primera vez que el sistema arranca.
        /// IMPORTANTE: Activa = false por defecto. El usuario decide.
        /// </summary>
        private static IEnumerable<Habilidad> Defaults() => new[]
        {
            new Habilidad
            {
                Clave         = HAB_MEMORIA,
                Nombre        = "Memoria del agente",
                Icono         = "🧠",
                Descripcion   = "Permite al agente recordar hechos sobre ti entre conversaciones y registrar episodios relevantes. Incluye lectura de memoria al inicio y escritura automática al final (Memorista).",
                ImpactoTokens = "+200–800 tokens por ejecución (según hechos guardados)",
                Activa        = false,
            },
            new Habilidad
            {
                Clave         = HAB_PATRONES,
                Nombre        = "Detección de patrones",
                Icono         = "🔎",
                Descripcion   = "Analiza Episodios.md en busca de tareas recurrentes (≥3 ocurrencias) y propone convertirlas en Skills ejecutables. Solo se dispara cuando tú entras al módulo de Patrones — no se ejecuta en cada pipeline.",
                ImpactoTokens = "+400–1200 tokens por análisis (solo al abrir el módulo)",
                Activa        = false,
            },
            new Habilidad
            {
                Clave         = HAB_MEMORIA_SEMANTICA,
                Nombre        = "Memoria semántica (RAG)",
                Icono         = "🧬",
                Descripcion   = "En lugar de inyectar TODA la memoria en cada prompt, usa embeddings para recuperar sólo los fragmentos más relevantes a la instrucción actual (top-K). Requiere que la habilidad 'Memoria' esté activa. Proveedores: Ollama (nomic-embed-text, local gratis) u OpenAI (text-embedding-3-small, barato).",
                ImpactoTokens = "Ahorro neto: −500 a −4000 tokens por instrucción cuando la memoria crece",
                Activa        = false,
            },
            new Habilidad
            {
                Clave         = HAB_TRACING,
                Nombre        = "Tracing de ejecución",
                Icono         = "🔬",
                Descripcion   = "Captura el árbol completo de cada instrucción (fases, LLM calls, herramientas, scripts, memoria) con tiempos, tokens, costos y previews de input/output. Persistencia JSONL en Traces/. Base para evaluación y replay de ejecuciones pasadas (Fase 1B).",
                ImpactoTokens = "0 tokens extra — solo disco y RAM (≈5–20 KB por ejecución)",
                Activa        = true,
            },
            new Habilidad
            {
                Clave         = HAB_CONTEXTO_SEMANTICO,
                Nombre        = "Contexto semántico del Constructor",
                Icono         = "🧩",
                Descripcion   = "En lugar de inyectar TODAS las credenciales, skills y automatizaciones en cada prompt del Constructor, usa embeddings para recuperar sólo los fragmentos relevantes a la instrucción actual (top-K por fuente). Incluye una tabla de contenidos ligera para que el LLM sepa qué existe. Si el retrieval falla, cae al dump completo clásico.",
                ImpactoTokens = "Ahorro neto: −2000 a −6000 tokens por instrucción cuando hay muchos skills/credenciales",
                Activa        = false,
            },
            new Habilidad
            {
                Clave         = HAB_HISTORIAL_COMPRIMIDO,
                Nombre        = "Historial comprimido",
                Icono         = "📚",
                Descripcion   = "Cuando la ventana de conversación expulsa turnos antiguos, en lugar de descartarlos los resume con un LLM económico y acumula el resumen. Mantiene coherencia multi-turno (el agente recuerda decisiones/archivos/objetivos) sin que cada turno pague el historial completo. Requiere al menos un proveedor configurado.",
                ImpactoTokens = "Ahorro neto: −1000 a −4000 tokens en conversaciones largas vs. historial clásico",
                Activa        = false,
            },
            // Espacio reservado:
            // new Habilidad { Clave = "sugerencias_proactivas", ... }
        };

        private HabilidadesRegistry() { }

        // ══════════════════ API pública ══════════════════

        /// <summary>
        /// Consulta rápida — usada en hot-path por el pipeline.
        /// Si el registry no se cargó aún, lo hace de forma perezosa.
        /// </summary>
        public bool EstaActiva(string clave)
        {
            AsegurarCargado();
            lock (_lock)
            {
                return _cache.TryGetValue(clave, out var h) && h.Activa;
            }
        }

        /// <summary>Devuelve todas las habilidades conocidas (para FrmHabilidades).</summary>
        public IReadOnlyList<Habilidad> Todas()
        {
            AsegurarCargado();
            lock (_lock)
            {
                return _cache.Values.OrderBy(h => h.Nombre).ToList();
            }
        }

        /// <summary>Cambia el estado de una habilidad y persiste.</summary>
        public void Establecer(string clave, bool activa)
        {
            AsegurarCargado();
            lock (_lock)
            {
                if (!_cache.TryGetValue(clave, out var h)) return;
                if (h.Activa == activa) return;
                h.Activa = activa;
                Persistir();
            }
        }

        /// <summary>Fuerza re-lectura del disco (por si se editó el JSON a mano).</summary>
        public void Recargar()
        {
            lock (_lock)
            {
                _cargado = false;
            }
            AsegurarCargado();
        }

        // ══════════════════ Infra privada ══════════════════

        private void AsegurarCargado()
        {
            lock (_lock)
            {
                if (_cargado) return;

                var desdeDisco = new List<Habilidad>();
                try
                {
                    desdeDisco = JsonManager.LeerOCrear<Habilidad>(
                        RutasProyecto.ObtenerRutaListHabilidades());
                }
                catch
                {
                    // Si el archivo está corrupto, partimos de defaults.
                    desdeDisco = new List<Habilidad>();
                }

                _cache.Clear();

                // Merge: defaults como base, luego override por lo que hay en disco.
                foreach (var d in Defaults())
                    _cache[d.Clave] = d;

                foreach (var h in desdeDisco)
                {
                    if (string.IsNullOrWhiteSpace(h.Clave)) continue;
                    if (_cache.TryGetValue(h.Clave, out var existente))
                    {
                        // Conservar solo el estado activo/inactivo del disco,
                        // la metadata (nombre, icono, descripción) viene del código.
                        existente.Activa = h.Activa;
                    }
                    else
                    {
                        // Habilidad desconocida en disco — la mantenemos por
                        // compatibilidad hacia atrás pero sin garantías.
                        _cache[h.Clave] = h;
                    }
                }

                _cargado = true;

                // Si el archivo no existía, lo materializamos ya con los defaults.
                Persistir();
            }
        }

        private void Persistir()
        {
            try
            {
                var lista = _cache.Values.ToList();
                JsonManager.Guardar(RutasProyecto.ObtenerRutaListHabilidades(), lista);
            }
            catch
            {
                // Si no se puede guardar, al menos el runtime tiene la decisión
                // correcta en caché. En la próxima ejecución volverá al default.
            }
        }
    }
}
