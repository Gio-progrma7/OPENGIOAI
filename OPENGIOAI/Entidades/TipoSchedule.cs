// ============================================================
//  TipoSchedule.cs — Enum tipado para la programación horaria
//
//  Reemplaza el uso de magic strings ("manual", "diaria",
//  "intervalo", "unica", "siempre") en el scheduler. La
//  Automatizacion sigue persistiendo TipoProgramacion como
//  string (para no romper compat JSON), pero el scheduler y
//  cualquier código nuevo deben trabajar con el enum.
// ============================================================

namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Tipos de programación de una <see cref="Automatizacion"/>.
    /// </summary>
    public enum TipoSchedule
    {
        /// <summary>Solo se ejecuta a petición del usuario (botón ▶).</summary>
        Manual,
        /// <summary>Se ejecuta cada día a la hora indicada.</summary>
        Diaria,
        /// <summary>Se ejecuta cada N minutos (opcionalmente dentro de un rango horario).</summary>
        Intervalo,
        /// <summary>Se ejecuta una sola vez en una fecha y hora concreta.</summary>
        Unica,
        /// <summary>Se ejecuta en cada ciclo del scheduler (cada 30s).</summary>
        Siempre
    }

    /// <summary>
    /// Conversión robusta entre <see cref="TipoSchedule"/> y la representación
    /// textual canónica usada en JSON (lowercase, sin tildes).
    /// </summary>
    public static class TipoScheduleHelper
    {
        /// <summary>
        /// Parsea un string a <see cref="TipoSchedule"/>. Tolera mayúsculas/
        /// minúsculas, tildes y null. Cualquier valor desconocido cae a Manual.
        /// </summary>
        public static TipoSchedule Parse(string? raw)
        {
            string s = (raw ?? "").Trim().ToLowerInvariant();
            return s switch
            {
                "diaria"             => TipoSchedule.Diaria,
                "intervalo"          => TipoSchedule.Intervalo,
                "unica" or "única"   => TipoSchedule.Unica,
                "siempre"            => TipoSchedule.Siempre,
                _                    => TipoSchedule.Manual,
            };
        }

        /// <summary>
        /// Forma canónica (lowercase, sin tildes) que se persiste en JSON.
        /// </summary>
        public static string ToCadena(this TipoSchedule t) => t switch
        {
            TipoSchedule.Diaria    => "diaria",
            TipoSchedule.Intervalo => "intervalo",
            TipoSchedule.Unica     => "unica",
            TipoSchedule.Siempre   => "siempre",
            _                      => "manual",
        };
    }
}
