// ============================================================
//  IServiciosComandos.cs
//
//  Fachada tipada de TODO lo que un command handler puede tocar
//  del estado de la app. Es el contrato entre los comandos y la
//  implementación concreta (FrmMandos lo implementa como partial).
//
//  POR QUÉ UNA FACHADA EN LUGAR DE PASAR EL FORM:
//    · Los handlers no saben que existe WinForms → testeables.
//    · Un cambio de UI (ej. migrar a WPF) no rompe los comandos.
//    · La superficie se documenta en un único lugar.
// ============================================================

using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosTTS;
using OPENGIOAI.Utilerias;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OPENGIOAI.Comandos
{
    public interface IServiciosComandos
    {
        // ── Selección de agente/modelo/ruta ──────────────────────────────────

        IReadOnlyList<Modelo>        ListaAgentes();
        IReadOnlyList<ModeloAgente>  ListaModelos();
        IReadOnlyList<Archivo>       ListaRutas();
        IReadOnlyList<Api>           ListaApis();

        string AgenteActual();   // Nombre del servicio (ChatGpt, Claude, ...)
        string ModeloActual();   // Ej: "gpt-4o-mini"
        string RutaActual();     // Path de la ruta de trabajo

        bool SeleccionarAgente(string nombre);
        bool SeleccionarModelo(string nombre);
        bool SeleccionarRuta(string nombre);

        void RecargarApis();

        // ── Toggles booleanos ────────────────────────────────────────────────

        bool RecordarTema          { get; set; }
        bool SoloChat              { get; set; }
        bool TelegramActivo        { get; set; }
        bool SlackActivo           { get; set; }
        bool AudioActivo           { get; set; }
        bool EnviarConstructorTg   { get; set; }
        bool EnviarConstructorSlack{ get; set; }
        bool EnviarArchivosTg      { get; set; }
        bool EnviarArchivosSlack   { get; set; }

        // ── Numéricos ────────────────────────────────────────────────────────

        /// <summary>Reintentos del Guardián (0–5).</summary>
        int Reintentos         { get; set; }

        /// <summary>Timeout del pipeline ARIA en segundos (mínimo 10).</summary>
        int TimeoutSegundos    { get; set; }

        // ── Audio / TTS ──────────────────────────────────────────────────────

        /// <summary>Snapshot de la configuración de audio actual.</summary>
        ConfiguracionTTS AudioConfigActual();

        /// <summary>
        /// Aplica una nueva configuración TTS y persiste.
        /// Devuelve true si el AudioTTSService la aceptó.
        /// </summary>
        bool AudioConfigurar(ConfiguracionTTS cfg);

        AudioTTSService AudioService { get; }

        // ── Acciones de pipeline ─────────────────────────────────────────────

        Task CancelarInstruccionAsync();

        // ── Transporte / mensajería ──────────────────────────────────────────

        BroadcastService Broadcast { get; }

        /// <summary>
        /// Muestra un mensaje en la zona de chat de la UI (burbuja "agente").
        /// Los comandos lo usan cuando el usuario ejecutó desde la UI y
        /// quieren dar feedback visible, o para mensajes de depuración.
        /// </summary>
        void MostrarEnUI(string mensaje);

        // ── Teclados Telegram reutilizables ──────────────────────────────────

        object KeyboardCambiarAgente();
        object KeyboardCambiarModelo();
        object KeyboardCambiarRuta();
        object KeyboardConfiguraciones();
    }
}
