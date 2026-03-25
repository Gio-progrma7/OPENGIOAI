// ============================================================
//  RetryPolicy.cs  — PASO 4
//
//  PROBLEMA ANTERIOR:
//    Cualquier error de red o de la API (429, 500, 503, timeout)
//    propagaba la excepción directamente al usuario sin ningún
//    intento de recuperación. Errores transitorios que se
//    resuelven solos en 1-2 segundos fallaban permanentemente.
//
//  SOLUCIÓN:
//    RetryPolicy.EjecutarAsync() envuelve cualquier llamada async
//    con reintentos automáticos y backoff exponencial con jitter.
//
//  COMPORTAMIENTO:
//    Intento 1 → falla → espera 1s  → reintento
//    Intento 2 → falla → espera 2s  → reintento
//    Intento 3 → falla → espera 4s  → reintento
//    Intento 4 → falla → lanza la excepción original
//
//    El jitter (±20% aleatorio) evita que múltiples agentes
//    en paralelo reintenten exactamente al mismo tiempo,
//    lo que agravaría un rate-limit en lugar de aliviarlo.
//
//  ERRORES REINTENTABLES:
//    - HttpRequestException  (red caída, timeout DNS)
//    - TaskCanceledException cuando NO es por CancellationToken
//      del usuario (es un timeout interno de HttpClient)
//    - Respuestas con código 429, 500, 502, 503, 504
//
//  ERRORES NO REINTENTABLES (se propagan inmediatamente):
//    - OperationCanceledException con el token del usuario
//      (el usuario canceló — respetar su decisión)
//    - 400 Bad Request (prompt inválido — reintentar no ayuda)
//    - 401 Unauthorized (API key incorrecta)
//    - 403 Forbidden
//    - Cualquier otra excepción de lógica de aplicación
//
//  USO:
//    string resultado = await RetryPolicy.EjecutarAsync(
//        () => ObtenerRespuestaAPIAsync(instruccion, ctx, ct),
//        ct,
//        onReintento: (intento, ex, espera) =>
//            Logger.Warn($"Reintento {intento}: {ex.Message} — esperando {espera.TotalSeconds}s")
//    );
// ============================================================

using System.Net;

namespace OPENGIOAI.Data
{
    public static class RetryPolicy
    {
        // ── Configuración por defecto ─────────────────────────────────────────
        public const int MaxReintentos = 3;
        public const double BaseSegundos = 1.0;   // espera base: 1s, 2s, 4s
        public const double JitterFactor = 0.20;  // ±20% aleatorio

        private static readonly Random _rng = new();

        // ── Códigos HTTP que justifican reintentar ────────────────────────────
        private static readonly HashSet<HttpStatusCode> CodigosReintentables = new()
        {
            HttpStatusCode.TooManyRequests,         // 429 — rate limit
            HttpStatusCode.InternalServerError,     // 500
            HttpStatusCode.BadGateway,              // 502
            HttpStatusCode.ServiceUnavailable,      // 503
            HttpStatusCode.GatewayTimeout,          // 504
        };

        /// <summary>
        /// Ejecuta <paramref name="operacion"/> con reintentos automáticos
        /// y backoff exponencial con jitter.
        ///
        /// Lanza la excepción original si se agotaron todos los reintentos
        /// o si el error no es reintentable.
        /// </summary>
        /// <param name="operacion">La llamada async a proteger.</param>
        /// <param name="ct">Token de cancelación del usuario.</param>
        /// <param name="maxReintentos">Máximo de reintentos (default: 3).</param>
        /// <param name="onReintento">
        /// Callback opcional para loggear cada reintento.
        /// Recibe: número de intento, excepción, tiempo de espera.
        /// </param>
        public static async Task<T> EjecutarAsync<T>(
            Func<Task<T>> operacion,
            CancellationToken ct = default,
            int maxReintentos = MaxReintentos,
            Action<int, Exception, TimeSpan>? onReintento = null)
        {
            Exception? ultimaExcepcion = null;

            for (int intento = 1; intento <= maxReintentos + 1; intento++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    return await operacion();
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // El usuario canceló explícitamente — no reintentar nunca.
                    throw;
                }
                catch (HttpRequestException ex)
                {
                    // Error de red o HTTP con código conocido
                    ultimaExcepcion = ex;

                    bool esReintentable = ex.StatusCode == null ||
                                         CodigosReintentables.Contains(ex.StatusCode.Value);

                    if (!esReintentable || intento > maxReintentos)
                        throw;
                }
                catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    // Timeout interno de HttpClient (no es cancelación del usuario)
                    ultimaExcepcion = ex;

                    if (intento > maxReintentos) throw;
                }
                catch (LlmErrorException ex) when (EsReintentable(ex.StatusCode))
                {
                    // Respuestas de error HTTP capturadas manualmente (ej: "Error 429: ...")
                    ultimaExcepcion = ex;

                    if (intento > maxReintentos) throw;
                }
                catch
                {
                    // Cualquier otra excepción (400, 401, lógica de app) — no reintentar.
                    throw;
                }

                // ── Calcular espera con backoff exponencial + jitter ──────────
                TimeSpan espera = CalcularEspera(intento);

                onReintento?.Invoke(intento, ultimaExcepcion!, espera);

                await Task.Delay(espera, ct);
            }

            // No debería llegar aquí, pero satisface al compilador
            throw ultimaExcepcion!;
        }

        /// <summary>
        /// Sobrecarga para operaciones que no devuelven valor (Task).
        /// </summary>
        public static Task EjecutarAsync(
            Func<Task> operacion,
            CancellationToken ct = default,
            int maxReintentos = MaxReintentos,
            Action<int, Exception, TimeSpan>? onReintento = null)
        {
            return EjecutarAsync<bool>(
                async () => { await operacion(); return true; },
                ct, maxReintentos, onReintento);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Backoff exponencial: BaseSegundos * 2^(intento-1) ± jitter.
        /// Intento 1 → ~1s | Intento 2 → ~2s | Intento 3 → ~4s
        /// Máximo capeado a 30s para no bloquear la UI demasiado.
        /// </summary>
        private static TimeSpan CalcularEspera(int intento)
        {
            double baseMs = BaseSegundos * Math.Pow(2, intento - 1) * 1000;
            double jitterMs = baseMs * JitterFactor * (2 * _rng.NextDouble() - 1);
            double totalMs = Math.Min(baseMs + jitterMs, 30_000);
            return TimeSpan.FromMilliseconds(Math.Max(totalMs, 100));
        }

        private static bool EsReintentable(HttpStatusCode? code) =>
            code.HasValue && CodigosReintentables.Contains(code.Value);
    }

    /// <summary>
    /// Excepción tipada para errores HTTP que vienen como string
    /// desde ObtenerRespuestaAPIAsync ("Error 429: ...").
    /// Permite que RetryPolicy identifique el código sin parsear strings.
    /// </summary>
    public sealed class LlmErrorException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public LlmErrorException(string message, HttpStatusCode? statusCode = null)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}