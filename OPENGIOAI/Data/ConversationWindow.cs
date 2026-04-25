// ============================================================
//  ConversationWindow.cs  — PASO 3 + Fase 2 (historial comprimido)
//
//  PROBLEMA ANTERIOR:
//    _scritpRespuesta en FrmMandos acumulaba todo el historial
//    de la sesión en un único string que crecía indefinidamente.
//    Con RecordarTema = true, cada vuelta concatenaba más texto:
//
//      _scritpRespuesta = "RESPUESTA DEL CODIGO :" + respuesta;
//      _scritpRespuesta += "\n\n RESPUESTA FINAL: " + respuesta;
//
//    En sesiones largas esto superaba el context window del modelo
//    y causaba truncamiento silencioso o errores de la API.
//    En memoria, el string nunca se liberaba.
//
//  SOLUCIÓN:
//    ConversationWindow mantiene una cola de turnos (usuario + agente).
//    Tiene dos límites configurables:
//      - MaxTurnos:  cuántos intercambios completos recordar (default 6)
//      - MaxTokens:  límite blando de tokens estimados (default 3000)
//    Cuando se agrega un turno nuevo que haría superar MaxTokens,
//    se descarta el turno más antiguo antes de agregar el nuevo.
//    El resultado es siempre una ventana de contexto controlada.
//
//  FASE 2 — HISTORIAL COMPRIMIDO:
//    Cuando la habilidad HAB_HISTORIAL_COMPRIMIDO está activa, los
//    turnos que salen de la ventana NO se descartan: se envían a un
//    resumidor (delegate) que los fusiona con el resumen previo. El
//    ResumenAcumulado se inyecta al inicio de ConstruirBloque() para
//    que el LLM recuerde decisiones/archivos/objetivos aunque los
//    turnos crudos ya se hayan expulsado.
//
//  USO EN FrmMandos:
//    Reemplaza completamente _scritpRespuesta.
//    Ver FrmMandos_ConversationWindow.cs para la integración.
// ============================================================

using OPENGIOAI.Utilerias;

namespace OPENGIOAI.Data
{
    /// <summary>
    /// Representa un turno completo de conversación:
    /// la instrucción del usuario y la respuesta del agente.
    /// </summary>
    public sealed record ConversationTurn(
        string Instruccion,
        string RespuestaAgente,
        DateTime Timestamp
    )
    {
        // Estimación rápida de tokens: 1 token ≈ 4 caracteres en español/inglés
        public int TokensEstimados =>
            (Instruccion.Length + RespuestaAgente.Length) / 4;
    }

    /// <summary>
    /// Delegado para resumir turnos expulsados de la ventana.
    /// Recibe el resumen previo y los turnos a integrar, devuelve el
    /// nuevo resumen fusionado. Si falla, debe devolver el previo.
    /// </summary>
    public delegate Task<string> ResumirHistorialDelegate(
        string resumenPrevio,
        IReadOnlyList<ConversationTurn> expulsados,
        CancellationToken ct);

    /// <summary>
    /// Ventana deslizante de contexto conversacional.
    /// Mantiene los últimos N turnos sin superar un límite de tokens.
    /// Thread-safe mediante lock ligero (las operaciones son O(N) con N pequeño).
    /// </summary>
    public sealed class ConversationWindow
    {
        private readonly LinkedList<ConversationTurn> _turnos = new();
        private readonly object _lock = new();

        /// <summary>
        /// Resumen acumulado de los turnos que ya salieron de la ventana.
        /// Vacío si la habilidad HAB_HISTORIAL_COMPRIMIDO nunca se activó
        /// o si aún no se ha expulsado ningún turno.
        /// </summary>
        public string ResumenAcumulado { get; private set; } = "";

        // ── Configuración ─────────────────────────────────────────────────────

        /// <summary>Máximo de turnos completos a retener.</summary>
        public int MaxTurnos { get; }

        /// <summary>
        /// Límite blando de tokens estimados.
        /// Si agregar un turno nuevo lo supera, se descarta el más antiguo.
        /// </summary>
        public int MaxTokens { get; }

        public ConversationWindow(int maxTurnos = 6, int maxTokens = 3000)
        {
            MaxTurnos = maxTurnos;
            MaxTokens = maxTokens;
        }

        // ── Estado ────────────────────────────────────────────────────────────

        /// <summary>Número de turnos actualmente en la ventana.</summary>
        public int Count
        {
            get { lock (_lock) return _turnos.Count; }
        }

        /// <summary>Tokens estimados totales de la ventana actual.</summary>
        public int TokensActuales
        {
            get { lock (_lock) return _turnos.Sum(t => t.TokensEstimados); }
        }

        /// <summary>True si la ventana no tiene ningún turno.</summary>
        public bool EstaVacia
        {
            get { lock (_lock) return _turnos.Count == 0; }
        }

        // ── Operaciones ───────────────────────────────────────────────────────

        /// <summary>
        /// Agrega un turno nuevo a la ventana.
        /// Si supera MaxTurnos o MaxTokens, descarta el turno más antiguo
        /// hasta que la ventana vuelva a estar dentro de los límites.
        /// NOTA: esta versión síncrona NO resume — los turnos expulsados
        /// se pierden. Usar AgregarAsync para conservarlos vía resumen.
        /// </summary>
        public void Agregar(string instruccion, string respuestaAgente)
        {
            if (string.IsNullOrWhiteSpace(instruccion) &&
                string.IsNullOrWhiteSpace(respuestaAgente))
                return;

            var turno = new ConversationTurn(
                instruccion, respuestaAgente, DateTime.Now);

            lock (_lock)
            {
                _turnos.AddLast(turno);

                // Respetar límite de turnos
                while (_turnos.Count > MaxTurnos)
                    _turnos.RemoveFirst();

                // Respetar límite de tokens (límite blando)
                // Solo descarta si hay más de 1 turno para no perder el último
                while (_turnos.Count > 1 &&
                       _turnos.Sum(t => t.TokensEstimados) > MaxTokens)
                    _turnos.RemoveFirst();
            }
        }

        /// <summary>
        /// Agrega un turno y, si la habilidad HAB_HISTORIAL_COMPRIMIDO está
        /// activa y se proporciona un resumidor, fusiona los turnos expulsados
        /// con el ResumenAcumulado previo. Si el resumidor falla, los turnos
        /// se pierden sin romper el flujo principal (el caller sigue adelante).
        /// </summary>
        public async Task AgregarAsync(
            string instruccion,
            string respuestaAgente,
            ResumirHistorialDelegate? resumidor,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(instruccion) &&
                string.IsNullOrWhiteSpace(respuestaAgente))
                return;

            var turno = new ConversationTurn(
                instruccion, respuestaAgente, DateTime.Now);

            List<ConversationTurn> expulsados = new();

            lock (_lock)
            {
                _turnos.AddLast(turno);

                while (_turnos.Count > MaxTurnos)
                {
                    expulsados.Add(_turnos.First!.Value);
                    _turnos.RemoveFirst();
                }

                while (_turnos.Count > 1 &&
                       _turnos.Sum(t => t.TokensEstimados) > MaxTokens)
                {
                    expulsados.Add(_turnos.First!.Value);
                    _turnos.RemoveFirst();
                }
            }

            if (expulsados.Count == 0) return;
            if (resumidor == null) return;
            if (!HabilidadesRegistry.Instancia.EstaActiva(
                    HabilidadesRegistry.HAB_HISTORIAL_COMPRIMIDO))
                return;

            try
            {
                string previo = ResumenAcumulado ?? "";
                string nuevo  = await resumidor(previo, expulsados, ct);
                if (!string.IsNullOrWhiteSpace(nuevo))
                    ResumenAcumulado = nuevo;
            }
            catch
            {
                // Tolerante a fallos: el resumidor ya loguea/trace internamente.
            }
        }

        /// <summary>
        /// Limpia toda la ventana y el resumen acumulado.
        /// Llamar cuando el usuario inicia una nueva conversación o
        /// cambia de agente/ruta.
        /// </summary>
        public void Limpiar()
        {
            lock (_lock) _turnos.Clear();
            ResumenAcumulado = "";
        }

        // ── Serialización a prompt ────────────────────────────────────────────

        /// <summary>
        /// Construye el bloque de contexto listo para inyectar en el prompt.
        /// Solo se incluye si hay al menos un turno previo.
        ///
        /// Formato compacto para no desperdiciar tokens:
        ///   [CONTEXTO PREVIO - N TURNOS]
        ///   ---
        ///   Usuario: ...
        ///   Agente:  ...
        ///   ---
        ///   [FIN CONTEXTO]
        /// </summary>
        public string ConstruirBloque()
        {
            lock (_lock)
            {
                bool hayResumen = !string.IsNullOrWhiteSpace(ResumenAcumulado);
                if (_turnos.Count == 0 && !hayResumen) return "";

                var sb = new System.Text.StringBuilder();

                if (hayResumen)
                {
                    sb.AppendLine("[RESUMEN PREVIO — contexto comprimido de turnos antiguos]");
                    sb.AppendLine(ResumenAcumulado!.Trim());
                    sb.AppendLine("[FIN RESUMEN]");
                    sb.AppendLine();
                }

                if (_turnos.Count == 0)
                    return sb.ToString();

                sb.AppendLine($"[CONTEXTO PREVIO — últimos {_turnos.Count} turno(s)]");

                foreach (var t in _turnos)
                {
                    sb.AppendLine("---");

                    if (!string.IsNullOrWhiteSpace(t.Instruccion))
                        sb.AppendLine("Usuario: " + Truncar(t.Instruccion, 400));

                    if (!string.IsNullOrWhiteSpace(t.RespuestaAgente))
                        sb.AppendLine("Agente:  " + Truncar(t.RespuestaAgente, 600));
                }

                sb.AppendLine("---");
                sb.AppendLine("[FIN CONTEXTO]");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Construye la instrucción final que se enviará al LLM,
        /// combinando el bloque de contexto con la instrucción actual.
        /// Si RecordarTema = false, devuelve la instrucción sin contexto.
        /// </summary>
        public string CombinarConInstruccion(
            string instruccionActual,
            bool recordarTema,
            bool soloChat)
        {
            var sb = new System.Text.StringBuilder();

            if (recordarTema && !EstaVacia)
            {
                sb.AppendLine(ConstruirBloque());
                sb.AppendLine();
                sb.AppendLine("NUEVA INSTRUCCIÓN:");
            }

            sb.Append(instruccionActual);

            // NOTA: El modo soloChat se comunica al LLM a través del contexto del agente
            // (AgentContext.soloChat), NO agregándolo al texto de la instrucción del usuario.
            // Antes se hacía " , SOLO CHAT = TRUE" aquí lo que corrompía el contenido
            // de tareas como "crea un archivo con el texto X" → escribía "X , SOLO CHAT = TRUE".

            return sb.ToString();
        }

        /// <summary>
        /// Snapshot de los turnos actuales (para debug o UI de historial).
        /// </summary>
        public IReadOnlyList<ConversationTurn> ObtenerTurnos()
        {
            lock (_lock) return _turnos.ToList().AsReadOnly();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Truncar(string texto, int maxChars)
        {
            if (texto.Length <= maxChars) return texto;
            return texto[..maxChars] + "…";
        }
    }
}