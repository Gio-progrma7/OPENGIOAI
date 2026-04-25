// ============================================================
//  CommandResult.cs
//
//  Resultado estructurado de un comando. El executor lo pasa al
//  formateador del canal correspondiente (Telegram/Slack/UI),
//  así un mismo handler puede renderizarse de forma óptima en
//  cada transporte sin duplicar lógica.
//
//  Diseño: Slack Block Kit y Discord Embeds inspiran este record.
// ============================================================

using System.Collections.Generic;

namespace OPENGIOAI.Comandos
{
    public sealed record CommandResult
    {
        /// <summary>True si el comando ejecutó correctamente.</summary>
        public bool Ok { get; init; } = true;

        /// <summary>Tipo semántico — dicta el ícono por defecto si no hay título.</summary>
        public ResultTipo Tipo { get; init; } = ResultTipo.Info;

        /// <summary>Título de una línea — opcional.</summary>
        public string? Titulo { get; init; }

        /// <summary>Mensaje principal del resultado. Puede ser null para comandos silenciosos.</summary>
        public string? Mensaje { get; init; }

        /// <summary>Bullets secundarios opcionales — aparecen bajo el mensaje principal.</summary>
        public IReadOnlyList<string>? Detalles { get; init; }

        /// <summary>
        /// Objeto adjunto para Telegram (reply_markup con inline_keyboard). Si es
        /// null no se adjunta teclado. Se ignora en Slack y en la UI.
        /// </summary>
        public object? TecladoTelegram { get; init; }

        /// <summary>
        /// Si es true, el executor NO enviará el resultado a ningún canal.
        /// Útil para comandos que ya manejaron su output (ej. abren un form).
        /// </summary>
        public bool Silencioso { get; init; } = false;

        // ── Constructores convenientes ───────────────────────────────────────

        public static CommandResult OkMsg(string mensaje, string? titulo = null) =>
            new() { Ok = true, Tipo = ResultTipo.Info, Titulo = titulo, Mensaje = mensaje };

        public static CommandResult Exito(string mensaje, string? titulo = null) =>
            new() { Ok = true, Tipo = ResultTipo.Exito, Titulo = titulo, Mensaje = mensaje };

        public static CommandResult Error(string mensaje, string? titulo = null) =>
            new() { Ok = false, Tipo = ResultTipo.Error, Titulo = titulo, Mensaje = mensaje };

        public static CommandResult Advertencia(string mensaje, string? titulo = null) =>
            new() { Ok = true, Tipo = ResultTipo.Advertencia, Titulo = titulo, Mensaje = mensaje };

        public static CommandResult Silencio() =>
            new() { Silencioso = true };
    }

    public enum ResultTipo
    {
        Info,
        Exito,
        Advertencia,
        Error,
    }

    public static class ResultTipoExt
    {
        public static string Icono(this ResultTipo t) => t switch
        {
            ResultTipo.Exito        => "✅",
            ResultTipo.Advertencia  => "⚠️",
            ResultTipo.Error        => "❌",
            _                        => "ℹ️",
        };
    }
}
