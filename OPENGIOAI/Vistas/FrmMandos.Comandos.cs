// ============================================================
//  FrmMandos.Comandos.cs
//
//  Implementación de `IServiciosComandos` como partial de FrmMandos.
//
//  POR QUÉ ESTÁ SEPARADO:
//    · Mantiene el grueso de FrmMandos.cs centrado en la lógica de
//      UI y orquestación de IA.
//    · Toda la superficie que los command handlers pueden tocar vive
//      aquí en un solo sitio — si un handler hace algo raro con la
//      UI, se encuentra rápido.
//    · Si en el futuro migráramos la UI (WPF, web) sólo este archivo
//      cambia; los handlers no se enteran.
//
//  CONVENCIONES:
//    · Todos los setters de toggles sincronizan el estado INTERNO
//      (_flag) con el control WinForms (checkBoxX.Checked) para que
//      la UI refleje cambios hechos desde Telegram/Slack.
//    · Los accesos a controles van envueltos en InvokeRequired-safe
//      helpers cuando el comando puede originarse en un hilo de
//      Telegram/Slack (callbacks async).
// ============================================================

using OPENGIOAI.Comandos;
using OPENGIOAI.Entidades;
using OPENGIOAI.ServiciosTelegram;
using OPENGIOAI.ServiciosTTS;
using OPENGIOAI.Utilerias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OPENGIOAI.Vistas
{
    public partial class FrmMandos : IServiciosComandos
    {
        // ── Helper de marshalling a UI thread ───────────────────────────────
        private void EnUIThread(Action accion)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(accion);
            else                accion();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  LISTAS DE REFERENCIA
        // ═══════════════════════════════════════════════════════════════════

        IReadOnlyList<Modelo>       IServiciosComandos.ListaAgentes() => _listaAgentes;
        IReadOnlyList<ModeloAgente> IServiciosComandos.ListaModelos() => _modelosAgente;
        IReadOnlyList<Archivo>      IServiciosComandos.ListaRutas()   => _listaArchivosDisponibles;
        IReadOnlyList<Api>          IServiciosComandos.ListaApis()    => _listaApisDisponibles;

        // ═══════════════════════════════════════════════════════════════════
        //  SELECCIÓN ACTUAL
        // ═══════════════════════════════════════════════════════════════════

        string IServiciosComandos.AgenteActual() =>
            _modeloSeleccionado?.Agente.ToString() ?? "";

        string IServiciosComandos.ModeloActual() =>
            _modeloSeleccionado?.Modelos ?? "";

        string IServiciosComandos.RutaActual() =>
            _archivoSeleccionado?.Ruta ?? "";

        bool IServiciosComandos.SeleccionarAgente(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return false;

            int svc = Utils.ObtenerServicio_Nombre(nombre);
            var agente = _listaAgentes.FirstOrDefault(e => (int)e.Agente == svc);
            if (agente == null)
            {
                // Permitir también match por nombre literal del enum.
                agente = _listaAgentes.FirstOrDefault(e =>
                    e.Agente.ToString().Equals(nombre, StringComparison.OrdinalIgnoreCase));
            }
            if (agente == null) return false;

            EnUIThread(() => comboBoxAgentes.SelectedValue = agente.ApiKey);
            return true;
        }

        bool IServiciosComandos.SeleccionarModelo(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return false;
            EnUIThread(() => comboBoxModeloIA.Text = nombre);
            return true;
        }

        bool IServiciosComandos.SeleccionarRuta(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return false;
            EnUIThread(() => comboBoxRuta.Text = nombre);
            return true;
        }

        void IServiciosComandos.RecargarApis() => CargarListaAgents();

        // ═══════════════════════════════════════════════════════════════════
        //  TOGGLES — sincronizan field + checkbox en UI thread
        // ═══════════════════════════════════════════════════════════════════

        bool IServiciosComandos.RecordarTema
        {
            get => _recordarTema;
            set
            {
                _recordarTema = value;
                EnUIThread(() =>
                {
                    checkBoxRecordar.Checked = value;
                    ChkConver.Visible = value;
                });
            }
        }

        bool IServiciosComandos.SoloChat
        {
            get => _soloChat;
            set
            {
                _soloChat = value;
                EnUIThread(() =>
                {
                    checkBoxSoloChat.Checked = value;
                    ChkEstado.Visible = value;
                });
            }
        }

        bool IServiciosComandos.TelegramActivo
        {
            get => _telegramActivo;
            set
            {
                _telegramActivo = value;
                EnUIThread(() => checkBoxTelegram.Checked = value);
            }
        }

        bool IServiciosComandos.SlackActivo
        {
            get => _slackActivo;
            set
            {
                _slackActivo = value;
                EnUIThread(() => checkBoxSlack.Checked = value);
            }
        }

        bool IServiciosComandos.AudioActivo
        {
            get => _enviarAudio;
            set
            {
                _enviarAudio = value;
                EnUIThread(() => checkBoxAudio.Checked = value);
            }
        }

        bool IServiciosComandos.EnviarConstructorTg
        {
            get => _enviarConstructorTelegram;
            set
            {
                _enviarConstructorTelegram = value;
                EnUIThread(() => checkBoxConstructorTelegram.Checked = value);
            }
        }

        bool IServiciosComandos.EnviarConstructorSlack
        {
            get => _enviarConstructorSlack;
            set
            {
                _enviarConstructorSlack = value;
                EnUIThread(() => checkBoxConstructorSlack.Checked = value);
            }
        }

        bool IServiciosComandos.EnviarArchivosTg
        {
            get => _enviarArchivosTelegram;
            set
            {
                _enviarArchivosTelegram = value;
                EnUIThread(() => checkBoxArchivosTelegram.Checked = value);
            }
        }

        bool IServiciosComandos.EnviarArchivosSlack
        {
            get => _enviarArchivosSlack;
            set
            {
                _enviarArchivosSlack = value;
                EnUIThread(() => checkBoxArchivosSlack.Checked = value);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  NUMÉRICOS
        // ═══════════════════════════════════════════════════════════════════

        int IServiciosComandos.Reintentos
        {
            get => (int)(_nudReintentos?.Value ?? 3);
            set
            {
                int v = Math.Max(0, Math.Min(5, value));
                EnUIThread(() =>
                {
                    if (_nudReintentos != null) _nudReintentos.Value = v;
                });
            }
        }

        int IServiciosComandos.TimeoutSegundos
        {
            get => _timeoutSegundos;
            set => _timeoutSegundos = Math.Max(TimeoutMinimo, Math.Min(TimeoutMaximo, value));
        }

        // ═══════════════════════════════════════════════════════════════════
        //  AUDIO / TTS
        // ═══════════════════════════════════════════════════════════════════

        ConfiguracionTTS IServiciosComandos.AudioConfigActual()
        {
            _audioService.RecargarConfig();
            var c = _audioService.Config;
            // Devolvemos una copia defensiva para evitar que un handler mute
            // el estado interno sin pasar por AudioConfigurar().
            return new ConfiguracionTTS
            {
                Proveedor = c.Proveedor,
                ApiKey    = c.ApiKey,
                Voz       = c.Voz,
                Idioma    = c.Idioma,
                Activo    = c.Activo,
            };
        }

        bool IServiciosComandos.AudioConfigurar(ConfiguracionTTS cfg)
        {
            if (cfg == null) return false;

            try
            {
                Utils.GuardarConfig(RutasProyecto.ObtenerRutaConfiguracionTTS(), cfg);
                _audioService.RecargarConfig();
                return true;
            }
            catch
            {
                return false;
            }
        }

        AudioTTSService IServiciosComandos.AudioService => _audioService;

        // ═══════════════════════════════════════════════════════════════════
        //  PIPELINE
        // ═══════════════════════════════════════════════════════════════════

        Task IServiciosComandos.CancelarInstruccionAsync()
        {
            EnUIThread(CancelarInstruccion);
            return Task.CompletedTask;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  TRANSPORTE
        // ═══════════════════════════════════════════════════════════════════

        BroadcastService IServiciosComandos.Broadcast => _broadcast;

        void IServiciosComandos.MostrarEnUI(string mensaje)
        {
            if (string.IsNullOrEmpty(mensaje)) return;
            EnUIThread(() => MostrarMensaje(mensaje, esUsuario: false));
        }

        // ═══════════════════════════════════════════════════════════════════
        //  TECLADOS TELEGRAM — delegan en TelegramSender con listas actuales
        // ═══════════════════════════════════════════════════════════════════

        object IServiciosComandos.KeyboardCambiarAgente() =>
            TelegramSender.CrearKeyboardDesdeListaAgentes(_listaAgentes);

        object IServiciosComandos.KeyboardCambiarModelo() =>
            TelegramSender.CrearKeyboardDesdeListaModelos(_modelosAgente);

        object IServiciosComandos.KeyboardCambiarRuta() =>
            TelegramSender.CrearKeyboardDesdeListaRutas(_listaArchivosDisponibles);

        object IServiciosComandos.KeyboardConfiguraciones() =>
            TelegramSender.Configuraciones_Menu();
    }
}
