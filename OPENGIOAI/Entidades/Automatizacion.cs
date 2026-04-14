using System;
using System.Collections.Generic;

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
        /// <summary>Tipo de programación: "unica", "diaria", "intervalo", "siempre"</summary>
        public string TipoProgramacion { get; set; } = "manual";

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
    }
}
