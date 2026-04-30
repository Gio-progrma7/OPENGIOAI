// ============================================================
//  VariableNodo.cs — Contrato tipado de E/S entre nodos
//
//  Cada nodo declara qué entradas espera recibir (de un nodo
//  predecesor o de las variables globales de la automatización)
//  y qué salidas produce. Esto reemplaza el flujo opaco de
//  NODO_ANTERIOR_RESULTADO (texto plano) por un contexto JSON
//  estructurado que el Python recibe vía la env var
//  NODO_CONTEXTO.
//
//  Análogo a SkillParametro pero específico de un grafo de
//  automatización (donde una variable puede venir de múltiples
//  nodos padres).
// ============================================================

using System.Collections.Generic;

namespace OPENGIOAI.Entidades
{
    /// <summary>
    /// Declaración tipada de una entrada o salida de un
    /// <see cref="NodoAutomatizacion"/>.
    /// </summary>
    public class VariableNodo
    {
        /// <summary>Nombre snake_case usado como clave en el contexto JSON.</summary>
        public string Nombre { get; set; } = "";

        /// <summary>JSON Schema type: string, number, integer, boolean, array, object.</summary>
        public string Tipo { get; set; } = "string";

        /// <summary>Descripción libre — útil para UI y para que el LLM entienda el contrato.</summary>
        public string Descripcion { get; set; } = "";

        /// <summary>Si es true, el grafo no puede ejecutarse sin esta variable.</summary>
        public bool Requerido { get; set; } = true;

        /// <summary>
        /// Valor por defecto si la variable no llega de un predecesor ni de las
        /// variables globales. Vacío = no aplica (y entonces Requerido=true falla).
        /// </summary>
        public string ValorPorDefecto { get; set; } = "";

        /// <summary>Lista cerrada de valores admitidos (enum). Vacío = libre.</summary>
        public List<string> Opciones { get; set; } = new();
    }
}
