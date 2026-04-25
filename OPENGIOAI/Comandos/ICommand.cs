// ============================================================
//  ICommand.cs
//
//  Contrato de un comando. Cada `#cmd` es una clase que
//  implementa esta interfaz y se registra en CommandRegistry.
//
//  PRINCIPIO: un comando = un archivo. Esto mantiene la lógica
//  localizada y facilita testear cada handler de forma aislada.
// ============================================================

using System.Threading.Tasks;

namespace OPENGIOAI.Comandos
{
    public interface ICommand
    {
        /// <summary>Metadata — nombre, alias, descripción, uso, ejemplos, categoría.</summary>
        CommandDescriptor Descriptor { get; }

        /// <summary>
        /// Ejecuta el comando. DEBE:
        ///   · Validar sus propios args (nunca lanzar por args inválidos —
        ///     devolver `CommandResult.Error(...)` en su lugar).
        ///   · Ser idempotente cuando sea posible.
        ///   · Evitar bloquear — usar Task.CompletedTask si no hay I/O.
        ///
        /// Las excepciones que sí escapen serán capturadas por el executor
        /// y convertidas en un Error visible al usuario.
        /// </summary>
        Task<CommandResult> EjecutarAsync(CommandContext ctx);
    }
}
