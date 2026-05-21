using OPENGIOAI.Entidades;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace OPENGIOAI.Themas
{
    /// <summary>
    /// Panel personalizado que sirve como canvas n8n-style.
    /// Dibuja curvas Bézier entre nodos conectados y gestiona
    /// el arrastre de nodos y la creación de conexiones.
    /// </summary>
    public class CanvasAutomatizacion : Panel
    {
        // ── Colores canvas ────────────────────────────────────────────────────
        private static readonly Color BgCanvas    = ColorTranslator.FromHtml("#080f1a");
        private static readonly Color GridColor   = Color.FromArgb(18, 255, 255, 255);
        private static readonly Color ArrowColor  = Color.FromArgb(180, 16, 185, 129);  // emerald
        private static readonly Color ArrowColorS = Color.FromArgb(255, 52, 211, 153);  // seleccionada

        // ── Estado ────────────────────────────────────────────────────────────
        private readonly List<NodoVisualControl> _nodos = new();
        private NodoVisualControl? _nodoSeleccionado;

        // ── Animación ─────────────────────────────────────────────────────────
        private readonly System.Windows.Forms.Timer _animationTimer;
        private float _dashOffset = 0f;

        // ── Zoom ──────────────────────────────────────────────────────────────
        private float _zoom = 1.0f;
        public  float Zoom  => _zoom;

        // Conexión en curso (drag desde punto de salida)
        private NodoVisualControl? _nodoOrigenConexion;
        private Point              _puntoRatonConexion;
        private bool               _modoConexion = false;

        // ── Eventos públicos ──────────────────────────────────────────────────
        public event EventHandler<NodoVisualControl>? NodoSeleccionado;
        public event EventHandler<(NodoVisualControl origen, NodoVisualControl destino)>? ConexionCreada;

        // ── Constructor ───────────────────────────────────────────────────────
        public CanvasAutomatizacion()
        {
            BackColor    = BgCanvas;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _animationTimer = new System.Windows.Forms.Timer { Interval = 40 }; // ~25 FPS
            _animationTimer.Tick += (s, e) =>
            {
                _dashOffset -= 1f; // Movimiento del flujo
                if (_dashOffset <= -100f) _dashOffset = 0f;
                Invalidate(false); // Redibujar sin borrar fondo
            };
            _animationTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // ── API pública ───────────────────────────────────────────────────────

        public NodoVisualControl AgregarNodo(NodoAutomatizacion datos)
        {
            var ctrl = new NodoVisualControl(datos);
            // Aplicar zoom actual al tamaño y posición del nodo
            ctrl.Zoom     = _zoom;
            ctrl.Size     = new Size((int)(NodoVisualControl.BaseWidth * _zoom), (int)(NodoVisualControl.BaseHeight * _zoom));
            ctrl.Location = new Point((int)(datos.CanvasX * _zoom), (int)(datos.CanvasY * _zoom));
            SuscribirNodo(ctrl);
            Controls.Add(ctrl);
            ctrl.BringToFront();
            _nodos.Add(ctrl);
            Invalidate();
            return ctrl;
        }

        // ── Zoom API ─────────────────────────────────────────────────────────
        public void AplicarZoom(float zoom)
        {
            zoom  = Math.Max(0.25f, Math.Min(3.0f, zoom));
            _zoom = zoom;
            foreach (var ctrl in _nodos)
            {
                ctrl.Zoom     = _zoom;
                ctrl.Size     = new Size((int)(NodoVisualControl.BaseWidth * _zoom), (int)(NodoVisualControl.BaseHeight * _zoom));
                ctrl.Location = new Point((int)(ctrl.Datos.CanvasX * _zoom), (int)(ctrl.Datos.CanvasY * _zoom));
            }
            Invalidate();
        }

        public void ZoomIn()    => AplicarZoom(_zoom + 0.1f);
        public void ZoomOut()   => AplicarZoom(_zoom - 0.1f);
        public void ResetZoom() => AplicarZoom(1.0f);

        public void EliminarNodo(NodoVisualControl nodo)
        {
            // Eliminar conexiones que apuntaban a este nodo
            foreach (var n in _nodos)
                n.Datos.ConexionesSalida.Remove(nodo.Datos.Id);

            Controls.Remove(nodo);
            _nodos.Remove(nodo);
            if (_nodoSeleccionado == nodo) _nodoSeleccionado = null;
            nodo.Dispose();
            Invalidate();
        }

        public void LimpiarCanvas()
        {
            foreach (var n in _nodos.ToList())
            {
                Controls.Remove(n);
                n.Dispose();
            }
            _nodos.Clear();
            _nodoSeleccionado = null;
            Invalidate();
        }

        public void CargarNodos(IEnumerable<NodoAutomatizacion> nodos)
        {
            LimpiarCanvas();
            foreach (var d in nodos)
                AgregarNodo(d);
        }

        public NodoVisualControl? NodoSeleccionadoActual => _nodoSeleccionado;

        public IReadOnlyList<NodoVisualControl> Nodos => _nodos;

        // ── Suscribir eventos del nodo ────────────────────────────────────────
        private void SuscribirNodo(NodoVisualControl ctrl)
        {
            ctrl.NodoSeleccionado += (s, n) => SeleccionarNodo(n);
            ctrl.NodoMovido       += (s, n) => Invalidate();
            ctrl.IniciarConexion  += (s, n) =>
            {
                _nodoOrigenConexion = n;
                _modoConexion = true;
                Cursor = Cursors.Cross;
            };
        }

        private void SeleccionarNodo(NodoVisualControl nodo)
        {
            if (_nodoSeleccionado != null && _nodoSeleccionado != nodo)
                _nodoSeleccionado.Seleccionado = false;

            _nodoSeleccionado = nodo;
            nodo.Seleccionado = true;
            NodoSeleccionado?.Invoke(this, nodo);
        }

        // ── Dibujo ────────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DibujarGrid(g);
            DibujarConexiones(g);

            // Línea temporal mientras arrastramos una conexión
            if (_modoConexion && _nodoOrigenConexion != null)
            {
                Point desde = _nodoOrigenConexion.PuntoSalidaAbsoluto;
                Point hasta  = _puntoRatonConexion;
                DibujarBezier(g, desde, hasta, Color.FromArgb(120, 16, 185, 129), dashed: true, animated: false, dashOffset: 0f);
            }
        }

        private void DibujarGrid(Graphics g)
        {
            int paso = (int)(28 * _zoom);
            if (paso < 10) return; // No dibujar si es muy denso

            using var brush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
            for (int x = 0; x < Width; x += paso)
            {
                for (int y = 0; y < Height; y += paso)
                {
                    g.FillRectangle(brush, x, y, 1.5f, 1.5f);
                }
            }
        }

        private void DibujarConexiones(Graphics g)
        {
            foreach (var nodoOrigen in _nodos)
            {
                foreach (var idDestino in nodoOrigen.Datos.ConexionesSalida)
                {
                    var nodoDestino = _nodos.FirstOrDefault(n => n.Datos.Id == idDestino);
                    if (nodoDestino == null) continue;

                    Point desde = nodoOrigen.PuntoSalidaAbsoluto;
                    Point hasta  = nodoDestino.PuntoEntradaAbsoluto;

                    bool esSeleccionada = _nodoSeleccionado == nodoOrigen ||
                                         _nodoSeleccionado == nodoDestino;

                    Color color = esSeleccionada ? ArrowColorS : ArrowColor;
                    // Animación tipo marching ants para conexiones establecidas
                    DibujarBezier(g, desde, hasta, color, dashed: false, animated: true, dashOffset: _dashOffset);
                    DibujarPuntaFlecha(g, hasta, color);
                }
            }
        }

        private static void DibujarBezier(Graphics g, Point desde, Point hasta, Color color, bool dashed, bool animated = false, float dashOffset = 0f)
        {
            int dx = Math.Abs(hasta.X - desde.X);
            int ctrl = Math.Max(60, dx / 2);

            Point c1 = new Point(desde.X + ctrl, desde.Y);
            Point c2 = new Point(hasta.X  - ctrl, hasta.Y);

            using var pen = new Pen(color, 2f);
            if (dashed) 
            {
                pen.DashStyle = DashStyle.Dash;
            }
            else if (animated)
            {
                pen.DashStyle = DashStyle.Custom;
                pen.DashPattern = new float[] { 5f, 5f };
                pen.DashOffset = dashOffset;
            }
            
            g.DrawBezier(pen, desde, c1, c2, hasta);
        }

        private static void DibujarPuntaFlecha(Graphics g, Point punta, Color color)
        {
            int sz = 7;
            Point[] tri =
            {
                new Point(punta.X,      punta.Y),
                new Point(punta.X - sz, punta.Y - sz / 2),
                new Point(punta.X - sz, punta.Y + sz / 2)
            };
            using var brush = new SolidBrush(color);
            g.FillPolygon(brush, tri);
        }

        // ── Mouse en el canvas (para modo conexión) ───────────────────────────
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_modoConexion)
            {
                _puntoRatonConexion = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_modoConexion && _nodoOrigenConexion != null)
            {
                // ¿Soltamos sobre otro nodo?
                var destino = _nodos.FirstOrDefault(n =>
                    n != _nodoOrigenConexion &&
                    n.Bounds.Contains(e.Location));

                if (destino != null)
                {
                    string idDestino = destino.Datos.Id;
                    if (!_nodoOrigenConexion.Datos.ConexionesSalida.Contains(idDestino))
                    {
                        _nodoOrigenConexion.Datos.ConexionesSalida.Add(idDestino);
                        ConexionCreada?.Invoke(this, (_nodoOrigenConexion, destino));
                    }
                }

                _modoConexion       = false;
                _nodoOrigenConexion = null;
                Cursor = Cursors.Default;
                Invalidate();
            }

            // Clic en vacío → deseleccionar
            var nodoBajoCursor = _nodos.FirstOrDefault(n => n.Bounds.Contains(e.Location));
            if (nodoBajoCursor == null && _nodoSeleccionado != null)
            {
                _nodoSeleccionado.Seleccionado = false;
                _nodoSeleccionado = null;
                NodoSeleccionado?.Invoke(this, null!);
            }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button == MouseButtons.Right)
            {
                var nodo = _nodos.FirstOrDefault(n => n.Bounds.Contains(e.Location));
                if (nodo != null)
                    MostrarMenuContextoNodo(nodo, e.Location);
            }
        }

        public void AutoLayout()
        {
            if (_nodos.Count == 0) return;

            var totalNodos = _nodos.Select(n => n.Datos).ToList();
            var enGrado = totalNodos.ToDictionary(n => n.Id, _ => 0);
            var idsValidos = new HashSet<string>(enGrado.Keys);
            
            foreach (var n in totalNodos)
            {
                foreach (var sucId in n.ConexionesSalida)
                {
                    if (idsValidos.Contains(sucId))
                        enGrado[sucId]++;
                }
            }

            var layers = totalNodos.ToDictionary(n => n.Id, _ => 0);
            var inDegreeCopy = enGrado.ToDictionary(kv => kv.Key, kv => kv.Value);
            var cola = new Queue<string>(inDegreeCopy.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            
            if (cola.Count == 0 && totalNodos.Count > 0)
            {
                cola.Enqueue(totalNodos.First().Id);
            }

            var visitados = new HashSet<string>();
            var nodosPorId = totalNodos.ToDictionary(n => n.Id);

            while (cola.Count > 0)
            {
                var currId = cola.Dequeue();
                if (visitados.Contains(currId)) continue;
                visitados.Add(currId);
                
                var currNode = nodosPorId[currId];
                var currLayer = layers[currId];

                foreach (var sucId in currNode.ConexionesSalida)
                {
                    if (!nodosPorId.ContainsKey(sucId)) continue;
                    
                    layers[sucId] = Math.Max(layers[sucId], currLayer + 1);
                    
                    inDegreeCopy[sucId]--;
                    if (inDegreeCopy[sucId] <= 0)
                    {
                        cola.Enqueue(sucId);
                    }
                }

                if (cola.Count == 0 && visitados.Count < totalNodos.Count)
                {
                    var noVisitado = inDegreeCopy.FirstOrDefault(kv => !visitados.Contains(kv.Key));
                    if (noVisitado.Key != null)
                    {
                        cola.Enqueue(noVisitado.Key);
                    }
                }
            }

            // Agrupar por capa
            var capas = new Dictionary<int, List<NodoAutomatizacion>>();
            foreach (var n in totalNodos)
            {
                int l = layers[n.Id];
                if (!capas.ContainsKey(l)) capas[l] = new List<NodoAutomatizacion>();
                capas[l].Add(n);
            }

            // Ordenar y espaciar
            int horizontalSpacing = 280; // BaseWidth (200) + 80
            int verticalSpacing = 120;   // BaseHeight (80) + 40
            int startX = 80;
            int centerY = 400; // Centrado en la parte superior del canvas

            var keys = capas.Keys.OrderBy(k => k).ToList();
            
            // Mapa de predecesores para ordenar verticalmente
            var predMap = totalNodos.ToDictionary(n => n.Id, _ => new List<string>());
            foreach (var n in totalNodos)
            {
                foreach (var sucId in n.ConexionesSalida)
                {
                    if (predMap.ContainsKey(sucId))
                        predMap[sucId].Add(n.Id);
                }
            }

            foreach (int capIndex in keys)
            {
                var nodosEnCapa = capas[capIndex];
                
                // Ordenar nodos dentro de la capa por el promedio de Y de sus predecesores
                if (capIndex > 0)
                {
                    nodosEnCapa = nodosEnCapa.OrderBy(n =>
                    {
                        var preds = predMap[n.Id];
                        if (preds.Count == 0) return 0f;
                        return (float)preds.Average(pId => nodosPorId[pId].CanvasY);
                    }).ToList();
                }

                int count = nodosEnCapa.Count;
                for (int i = 0; i < count; i++)
                {
                    var nodo = nodosEnCapa[i];
                    nodo.CanvasX = startX + capIndex * horizontalSpacing;
                    nodo.CanvasY = centerY - ((count - 1) * verticalSpacing) / 2 + i * verticalSpacing;
                    
                    if (nodo.CanvasY < 50) nodo.CanvasY = 50;
                }
            }

            // Actualizar controles visuales
            AplicarZoom(_zoom);
            Invalidate();
        }

        private void MostrarMenuContextoNodo(NodoVisualControl nodo, Point pos)
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = ColorTranslator.FromHtml("#111827");
            menu.ForeColor = Color.White;

            var itemEliminar = new ToolStripMenuItem("🗑 Eliminar nodo");
            itemEliminar.Click += (s, e) => EliminarNodo(nodo);
            menu.Items.Add(itemEliminar);

            var itemDesconectar = new ToolStripMenuItem("✂ Desconectar salidas");
            itemDesconectar.Click += (s, e) =>
            {
                nodo.Datos.ConexionesSalida.Clear();
                Invalidate();
            };
            menu.Items.Add(itemDesconectar);

            // Submenú "Conectar a…" con todos los nodos disponibles
            var otrosNodos = _nodos.Where(n => n != nodo &&
                !nodo.Datos.ConexionesSalida.Contains(n.Datos.Id)).ToList();
            if (otrosNodos.Count > 0)
            {
                var itemConectar = new ToolStripMenuItem("🔗 Conectar a…");
                foreach (var destino in otrosNodos)
                {
                    var d = destino; // captura para closure
                    var sub = new ToolStripMenuItem($"{d.Datos.Titulo} (Paso {d.Datos.OrdenEjecucion + 1})");
                    sub.Click += (s2, e2) =>
                    {
                        nodo.Datos.ConexionesSalida.Add(d.Datos.Id);
                        ConexionCreada?.Invoke(this, (nodo, d));
                        Invalidate();
                    };
                    itemConectar.DropDownItems.Add(sub);
                }
                menu.Items.Add(itemConectar);
            }

            menu.Show(this, pos);
        }
    }
}
