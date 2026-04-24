using OPENGIOAI.Themas;
using System;
using System.Windows.Forms;

namespace OPENGIOAI.Utilerias
{
    /// <summary>
    /// Throttle de actualizaciones para burbujas de chat en streaming. Agrupa
    /// los cambios de texto en ticks de ~120 ms para no saturar el hilo UI
    /// cuando llegan muchos tokens por segundo.
    ///
    /// El servicio apunta a una burbuja a la vez; el consumidor crea la burbuja
    /// y la registra con <see cref="Apuntar"/>, acumula texto con
    /// <see cref="AcumularTexto"/> (thread-safe) y cierra con
    /// <see cref="Finalizar"/>.
    /// </summary>
    public class ChatStreamingThrottleService : IDisposable
    {
        private readonly Control _uiControl;
        private readonly System.Windows.Forms.Timer _timer;

        private volatile bool _pendiente;
        private BurbujaChat? _burbujaActual;
        private string _textoActual = "";

        public ChatStreamingThrottleService(Control uiControl, int intervaloMs = 120)
        {
            _uiControl = uiControl;
            _timer = new System.Windows.Forms.Timer { Interval = intervaloMs };
            _timer.Tick += OnTick;
        }

        /// <summary>Burbuja activa del throttle (null si no hay streaming en curso).</summary>
        public BurbujaChat? BurbujaActual => _burbujaActual;

        private void OnTick(object? sender, EventArgs e)
        {
            if (!_pendiente) return;
            _pendiente = false;

            if (_burbujaActual == null || _burbujaActual.IsDisposed) return;
            _burbujaActual.ActualizarTexto(_textoActual);

            if (_burbujaActual.Parent is ScrollableControl sc)
                sc.ScrollControlIntoView(_burbujaActual);
        }

        /// <summary>
        /// Apunta el throttle a una nueva burbuja y arranca el timer. Debe
        /// llamarse desde el hilo UI.
        /// </summary>
        public void Apuntar(BurbujaChat burbuja)
        {
            _burbujaActual = burbuja;
            _textoActual = "";
            _pendiente = false;
            _timer.Start();
        }

        /// <summary>
        /// Registra texto pendiente para el próximo tick. Seguro desde
        /// cualquier hilo: solo escribe campos (uno volátil, otro publicado
        /// por el mismo write-lock implícito del CLR).
        /// </summary>
        public void AcumularTexto(string texto)
        {
            _textoActual = texto;
            _pendiente = true;
        }

        /// <summary>
        /// Detiene el timer, flushea el texto final a la burbuja y la libera.
        /// Si <paramref name="textoFinal"/> es null o vacío, respeta el último
        /// texto mostrado.
        /// </summary>
        public void Finalizar(string? textoFinal)
        {
            if (_uiControl.InvokeRequired)
            {
                _uiControl.Invoke(() => Finalizar(textoFinal));
                return;
            }

            _timer.Stop();
            _pendiente = false;

            if (_burbujaActual == null || _burbujaActual.IsDisposed)
            {
                _burbujaActual = null;
                return;
            }

            if (!string.IsNullOrEmpty(textoFinal))
                _burbujaActual.ActualizarTexto(textoFinal);

            if (_burbujaActual.Parent is ScrollableControl sc)
                sc.ScrollControlIntoView(_burbujaActual);

            _burbujaActual = null;
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
