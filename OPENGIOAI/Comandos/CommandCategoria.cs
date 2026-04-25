// ============================================================
//  CommandCategoria.cs
//
//  Clasificación de los comandos para agruparlos en el menú de
//  ayuda. El icono acompaña al título de categoría en la salida
//  de `#ayuda`, igual que hacen Slack/Discord con sus slash commands.
// ============================================================

namespace OPENGIOAI.Comandos
{
    public enum CommandCategoria
    {
        /// <summary>🤖 Agente, modelo, ruta — qué motor trabaja.</summary>
        Agente,
        /// <summary>⚙️ Toggles y numéricos de comportamiento.</summary>
        Configuracion,
        /// <summary>🧠 Habilidades cognitivas del agente.</summary>
        Habilidades,
        /// <summary>📡 Telegram, Slack, audio — por dónde se comunica.</summary>
        Integracion,
        /// <summary>📊 Estado actual y acciones de control.</summary>
        Estado,
        /// <summary>❓ Ayuda y descubrimiento.</summary>
        Ayuda,
    }

    public static class CommandCategoriaExt
    {
        public static string Icono(this CommandCategoria c) => c switch
        {
            CommandCategoria.Agente        => "🤖",
            CommandCategoria.Configuracion => "⚙️",
            CommandCategoria.Habilidades   => "🧠",
            CommandCategoria.Integracion   => "📡",
            CommandCategoria.Estado        => "📊",
            CommandCategoria.Ayuda         => "❓",
            _                               => "•",
        };

        public static string Titulo(this CommandCategoria c) => c switch
        {
            CommandCategoria.Agente        => "Agente",
            CommandCategoria.Configuracion => "Configuración",
            CommandCategoria.Habilidades   => "Habilidades",
            CommandCategoria.Integracion   => "Integración",
            CommandCategoria.Estado        => "Estado",
            CommandCategoria.Ayuda         => "Ayuda",
            _                               => c.ToString(),
        };
    }
}
