using System;
using System.Collections.Generic;
using System.Linq;

namespace OPENGIOAI.Entidades
{
    public class Automatizacion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nombre { get; set; } = "Nueva automatización";
        public string Descripcion { get; set; } = "";
        public bool Activa { get; set; } = true;
        public List<NodoAutomatizacion> Nodos { get; set; } = new();
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public string UltimoEstado { get; set; } = "Sin ejecutar";
        public DateTime? UltimaEjecucion { get; set; } = null;

        // ── Programación horaria ──────────────────────────────────────────
        /// <summary>
        /// Tipo de programación, persistido como string para compatibilidad
        /// con JSON existente. Para lógica nueva, usar <see cref="TipoScheduleEnum"/>.
        /// Valores canónicos: "manual", "diaria", "intervalo", "unica", "siempre".
        /// </summary>
        public string TipoProgramacion { get; set; } = "manual";

        /// <summary>
        /// Acceso tipado a <see cref="TipoProgramacion"/>. Set normaliza el string
        /// subyacente a la forma canónica. No se serializa (la fuente es el string).
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public TipoSchedule TipoScheduleEnum
        {
            get => TipoScheduleHelper.Parse(TipoProgramacion);
            set => TipoProgramacion = value.ToCadena();
        }

        /// <summary>
        /// Timeout máximo de la ejecución completa (toda la cadena de nodos).
        /// 0 = sin límite. Por defecto 15 minutos, igual que el viejo hardcoded.
        /// </summary>
        public int TimeoutMinutos { get; set; } = 15;

        /// <summary>Hora de ejecución (para "unica" y "diaria"). Formato "HH:mm"</summary>
        public string HoraEjecucion { get; set; } = "";

        /// <summary>Hora de inicio del rango (para "rango"). Formato "HH:mm"</summary>
        public string HoraInicio { get; set; } = "";

        /// <summary>Hora de fin del rango. Formato "HH:mm"</summary>
        public string HoraFin { get; set; } = "";

        /// <summary>Intervalo en minutos entre ejecuciones (para "intervalo")</summary>
        public int IntervaloMinutos { get; set; } = 60;

        /// <summary>Días de la semana habilitados (0=Dom ... 6=Sab). Vacío = todos.</summary>
        public List<int> DiasActivos { get; set; } = new();

        /// <summary>Fecha de ejecución única (para tipo "unica").</summary>
        public DateTime FechaUnica { get; set; } = DateTime.Today;

        /// <summary>Carpeta propia donde se guardan los scripts de esta automatización.</summary>
        public string CarpetaScripts { get; set; } = "";

        /// <summary>
        /// Variables globales de la automatización (constantes, secrets, paths, etc.).
        /// Disponibles a todos los nodos vía la env var <c>AUTO_VARIABLES</c>
        /// (JSON serializado). También cubren entradas requeridas si el nombre coincide.
        /// </summary>
        public Dictionary<string, string> Variables { get; set; } = new();

        // ── Validación del grafo ──────────────────────────────────────────

        /// <summary>
        /// Valida que el grafo sea ejecutable: sin ciclos y con todas las
        /// entradas requeridas resolvibles desde un predecesor, una variable
        /// global o un valor por defecto. Devuelve la lista de errores;
        /// vacía = grafo válido.
        /// </summary>
        public List<string> ValidarGrafo()
        {
            var errores = new List<string>();
            if (Nodos == null || Nodos.Count == 0)
            {
                errores.Add("La automatización no tiene nodos.");
                return errores;
            }

            // 1) Ciclos (Kahn)
            var enGrado  = Nodos.ToDictionary(n => n.Id, _ => 0);
            var idsValidos = new HashSet<string>(enGrado.Keys);
            foreach (var n in Nodos)
                foreach (var sucId in n.ConexionesSalida)
                    if (idsValidos.Contains(sucId)) enGrado[sucId]++;

            var cola = new Queue<string>(enGrado.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            int procesados = 0;
            var nodosPorId = Nodos.ToDictionary(n => n.Id);
            while (cola.Count > 0)
            {
                string id = cola.Dequeue();
                procesados++;
                foreach (var sucId in nodosPorId[id].ConexionesSalida)
                {
                    if (!enGrado.ContainsKey(sucId)) continue;
                    if (--enGrado[sucId] == 0) cola.Enqueue(sucId);
                }
            }
            if (procesados < Nodos.Count)
            {
                var enCiclo = enGrado.Where(kv => kv.Value > 0)
                    .Select(kv => nodosPorId[kv.Key].Titulo).ToList();
                errores.Add($"Ciclo detectado en el grafo (nodos: {string.Join(", ", enCiclo)}).");
                return errores; // sin orden topológico no podemos validar entradas
            }

            // 2) Predecesores por nodo
            var predMap = Nodos.ToDictionary(n => n.Id, _ => new List<string>());
            foreach (var n in Nodos)
                foreach (var sucId in n.ConexionesSalida)
                    if (predMap.ContainsKey(sucId)) predMap[sucId].Add(n.Id);

            var variablesGlobales = new HashSet<string>(
                Variables?.Keys ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            // 3) Cada entrada requerida debe estar disponible
            foreach (var nodo in Nodos)
            {
                if (nodo.Entradas == null || nodo.Entradas.Count == 0) continue;

                var salidasDePredecesores = new HashSet<string>(
                    predMap[nodo.Id]
                        .Select(pid => nodosPorId[pid])
                        .SelectMany(p => p.Salidas ?? new List<VariableNodo>())
                        .Select(s => s.Nombre),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var entrada in nodo.Entradas)
                {
                    if (!entrada.Requerido) continue;
                    if (!string.IsNullOrEmpty(entrada.ValorPorDefecto)) continue;

                    bool resoluble = salidasDePredecesores.Contains(entrada.Nombre)
                                  || variablesGlobales.Contains(entrada.Nombre);
                    if (!resoluble)
                        errores.Add(
                            $"Nodo '{nodo.Titulo}': falta la entrada requerida '{entrada.Nombre}' " +
                            $"(no la produce ningún predecesor ni está en variables globales).");
                }
            }

            return errores;
        }
    }
}
