// ============================================================
//  ConversationWindow.cs  — PASO 3
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
//  USO EN FrmMandos:
//    Reemplaza completamente _scritpRespuesta.
//    Ver FrmMandos_ConversationWindow.cs para la integración.
// ============================================================

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
    /// Ventana deslizante de contexto conversacional.
    /// Mantiene los últimos N turnos sin superar un límite de tokens.
    /// Thread-safe mediante lock ligero (las operaciones son O(N) con N pequeño).
    /// </summary>
    public sealed class ConversationWindow
    {
        private readonly LinkedList<ConversationTurn> _turnos = new();
        private readonly object _lock = new();

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
        /// Limpia toda la ventana.
        /// Llamar cuando el usuario inicia una nueva conversación o
        /// cambia de agente/ruta.
        /// </summary>
        public void Limpiar()
        {
            lock (_lock) _turnos.Clear();
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
                if (_turnos.Count == 0) return "";

                var sb = new System.Text.StringBuilder();
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