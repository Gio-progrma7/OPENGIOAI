// ============================================================
//  ConsumoTokensTracker.cs
//
//  Singleton que registra cada llamada al LLM y las agrupa por
//  "ejecución" (una instrucción del usuario que atraviesa varias
//  fases del pipeline). Es la fuente de datos de FrmConsumoTokens.
//
//  CICLO DE VIDA POR EJECUCIÓN:
//    1. OrquestadorARIA.EjecutarAsync llama a IniciarEjecucion(instruccion)
//       al principio → se genera un InstruccionId y se crea un "bucket".
//    2. Cada fase del pipeline hace ctx.ComoFase("Analista") etc., y
//       AIModelConector, al recibir la respuesta, llama a Registrar(...)
//       que enrutará el consumo al bucket activo.
//    3. FinalizarEjecucion() mueve el bucket al historial y notifica
//       a los suscriptores (la UI).
//
//  CONCURRENCIA:
//    La app corre un pipeline a la vez (el usuario da una instrucción,
//    espera respuesta). Aún así, todos los accesos al estado están
//    bajo _lock y los eventos se disparan FUERA del lock para que los
//    handlers puedan tocar la UI sin deadlockear.
//
//  EVENTOS (para la UI):
//    · OnConsumoRegistrado(ConsumoTokens) → cada vez que llega una llamada.
//    · OnEjecucionFinalizada(EjecucionConsumo) → al cerrar un bucket.
//
//  MEMORIA:
//    Historial acotado a las últimas 100 ejecuciones. Suficiente para
//    la UI y evita crecer sin freno durante sesiones largas.
// ============================================================

using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OPENGIOAI.Utilerias
{
    /// <summary>Agrupa todos los consumos de una misma instrucción del usuario.</summary>
    public class EjecucionConsumo
    {
        public string InstruccionId { get; set; } = "";
        public string Instruccion { get; set; } = "";
        public DateTime Inicio { get; set; } = DateTime.Now;
        public DateTime? Fin { get; set; }
        public List<ConsumoTokens> Llamadas { get; set; } = new();

        public int TotalPromptTokens     => Llamadas.Sum(c => c.PromptTokens);
        public int TotalCompletionTokens => Llamadas.Sum(c => c.CompletionTokens);
        public int TotalTokens           => Llamadas.Sum(c => c.TotalTokens);
        public decimal CostoEstimadoUsd  => Llamadas.Sum(c => c.CostoEstimadoUsd);
    }

    public sealed class ConsumoTokensTracker
    {
        private static readonly ConsumoTokensTracker _instancia = new();
        public static ConsumoTokensTracker Instancia => _instancia;

        private const int MaxHistorial = 100;

        private readonly object _lock = new();
        private EjecucionConsumo? _actual;
        private readonly LinkedList<EjecucionConsumo> _historial = new();

        // ══════════════════ Eventos ══════════════════

        public event Action<ConsumoTokens>?    OnConsumoRegistrado;
        public event Action<EjecucionConsumo>? OnEjecucionIniciada;
        public event Action<EjecucionConsumo>? OnEjecucionFinalizada;

        private ConsumoTokensTracker() { }

        // ══════════════════ API pública ══════════════════

        /// <summary>
        /// Abre un nuevo bucket de ejecución. Devuelve el InstruccionId
        /// generado — el orquestador NO necesita guardarlo, el tracker lo
        /// recuerda como "actual" hasta FinalizarEjecucion.
        /// </summary>
        public string IniciarEjecucion(string instruccion)
        {
            var ejec = new EjecucionConsumo
            {
                InstruccionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                Instruccion   = Recortar(instruccion, 400),
                Inicio        = DateTime.Now,
            };

            lock (_lock)
            {
                // Si había una ejecución abierta sin cerrar (ej. error en pipeline),
                // la archivamos sin romper nada.
                if (_actual != null)
                    ArchivarInterno(_actual);
                _actual = ejec;
            }

            try { OnEjecucionIniciada?.Invoke(ejec); } catch { }
            return ejec.InstruccionId;
        }

        /// <summary>
        /// Registra el consumo de UNA llamada al LLM. Si no hay ejecución
        /// activa, la llamada se atribuye a una ejecución "libre" implícita
        /// (útil para llamadas sueltas como Memorista fuera del pipeline).
        /// </summary>
        public void Registrar(ConsumoTokens consumo)
        {
            if (consumo == null) return;

            // Enriquecer con costo antes de registrar.
            if (consumo.CostoEstimadoUsd == 0m)
            {
                consumo.CostoEstimadoUsd = PreciosModelos.Instancia.Estimar(
                    consumo.Modelo,
                    consumo.PromptTokens,
                    consumo.CompletionTokens);
            }

            lock (_lock)
            {
                if (_actual == null)
                {
                    _actual = new EjecucionConsumo
                    {
                        InstruccionId = Guid.NewGuid().ToString("N").Substring(0, 12),
                        Instruccion   = "(llamada suelta)",
                        Inicio        = DateTime.Now,
                    };
                }
                consumo.InstruccionId = _actual.InstruccionId;
                _actual.Llamadas.Add(consumo);
            }

            try { OnConsumoRegistrado?.Invoke(consumo); } catch { }
        }

        /// <summary>Cierra el bucket actual y lo mueve al historial.</summary>
        public void FinalizarEjecucion()
        {
            EjecucionConsumo? cerrada = null;
            lock (_lock)
            {
                if (_actual == null) return;
                _actual.Fin = DateTime.Now;
                cerrada = _actual;
                ArchivarInterno(_actual);
                _actual = null;
            }

            if (cerrada != null)
            {
                try { OnEjecucionFinalizada?.Invoke(cerrada); } catch { }
            }
        }

        /// <summary>Snapshot de la ejecución actual (o null si no hay).</summary>
        public EjecucionConsumo? EjecucionActual()
        {
            lock (_lock) { return _actual; }
        }

        /// <summary>Snapshot del historial (más reciente primero).</summary>
        public IReadOnlyList<EjecucionConsumo> Historial()
        {
            lock (_lock)
            {
                return _historial.ToList();
            }
        }

        /// <summary>Total acumulado de la sesión (historial + actual).</summary>
        public (int tokens, decimal usd) TotalSesion()
        {
            lock (_lock)
            {
                int tokens = _historial.Sum(e => e.TotalTokens);
                decimal usd = _historial.Sum(e => e.CostoEstimadoUsd);
                if (_actual != null)
                {
                    tokens += _actual.TotalTokens;
                    usd    += _actual.CostoEstimadoUsd;
                }
                return (tokens, usd);
            }
        }

        /// <summary>Limpia el historial (no toca la ejecución en curso).</summary>
        public void Limpiar()
        {
            lock (_lock) { _historial.Clear(); }
        }

        // ══════════════════ Infra privada ══════════════════

        private void ArchivarInterno(EjecucionConsumo ejec)
        {
            _historial.AddFirst(ejec);
            while (_historial.Count > MaxHistorial)
                _historial.RemoveLast();
        }

        private static string Recortar(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
