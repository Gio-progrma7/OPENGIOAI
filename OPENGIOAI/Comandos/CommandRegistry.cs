// ============================================================
//  CommandRegistry.cs
//
//  Almacena todos los ICommand de la app indexados por su
//  nombre canónico y sus alias. Soporta:
//    · Lookup exacto (case-insensitive).
//    · Enumeración agrupada por categoría para `#ayuda`.
//    · Sugerencias por typo (Levenshtein ≤ 2) para mensajes
//      de error útiles en lugar de caer al LLM.
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace OPENGIOAI.Comandos
{
    public sealed class CommandRegistry
    {
        // Un único Dictionary que mapea cada nombre reconocido (canónico o alias)
        // al comando. Así el lookup es O(1) independientemente del alias usado.
        private readonly Dictionary<string, ICommand> _porNombre =
            new(StringComparer.OrdinalIgnoreCase);

        // Conjunto único de comandos — usado para enumeración y ayuda.
        private readonly HashSet<ICommand> _todos = new();

        public void Registrar(ICommand cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            _todos.Add(cmd);

            foreach (var nombre in cmd.Descriptor.NombresReconocidos())
            {
                if (string.IsNullOrWhiteSpace(nombre)) continue;
                _porNombre[nombre] = cmd;
            }
        }

        /// <summary>Devuelve el comando que responde a `nombre` o null.</summary>
        public ICommand? Resolver(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return null;
            _porNombre.TryGetValue(nombre.Trim().ToLowerInvariant(), out var cmd);
            return cmd;
        }

        /// <summary>Todos los comandos registrados (sin duplicar por alias).</summary>
        public IReadOnlyList<ICommand> Todos() =>
            _todos.OrderBy(c => c.Descriptor.Nombre).ToList();

        /// <summary>Solo los comandos visibles — excluye los marcados como Oculto.</summary>
        public IReadOnlyList<ICommand> TodosVisibles() =>
            _todos.Where(c => !c.Descriptor.Oculto)
                  .OrderBy(c => c.Descriptor.Nombre)
                  .ToList();

        /// <summary>
        /// Devuelve hasta `max` comandos cuyo nombre canónico o alias está
        /// a distancia Levenshtein ≤ `umbral` del input. Ordenado por cercanía.
        /// Usado para "¿quisiste decir `#habilidad`?"
        /// </summary>
        public IReadOnlyList<ICommand> Sugerencias(string input, int umbral = 2, int max = 3)
        {
            if (string.IsNullOrWhiteSpace(input)) return System.Array.Empty<ICommand>();

            string q = input.Trim().ToLowerInvariant();

            var rankeados = new List<(ICommand cmd, int dist)>();
            foreach (var cmd in _todos)
            {
                if (cmd.Descriptor.Oculto) continue;

                int mejor = int.MaxValue;
                foreach (var nombre in cmd.Descriptor.NombresReconocidos())
                {
                    int d = Levenshtein(q, nombre);
                    if (d < mejor) mejor = d;
                }
                if (mejor <= umbral) rankeados.Add((cmd, mejor));
            }

            return rankeados
                .OrderBy(t => t.dist)
                .ThenBy(t => t.cmd.Descriptor.Nombre)
                .Take(max)
                .Select(t => t.cmd)
                .ToList();
        }

        // ── Levenshtein clásica — matriz completa ────────────────────────────

        private static int Levenshtein(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            int[] prev = new int[b.Length + 1];
            int[] curr = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++) prev[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int costo = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    curr[j] = System.Math.Min(
                        System.Math.Min(curr[j - 1] + 1, prev[j] + 1),
                        prev[j - 1] + costo);
                }
                (prev, curr) = (curr, prev);
            }
            return prev[b.Length];
        }
    }
}
