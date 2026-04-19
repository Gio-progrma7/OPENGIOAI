// ============================================================
//  PatronDetectado.cs
//
//  Representa un patrón recurrente detectado en Episodios.md —
//  una tarea que el usuario ha pedido al agente varias veces
//  (≥3 ocurrencias) y que podría convertirse en un Skill.
//
//  CICLO DE VIDA:
//    1. DetectorPatrones agrupa líneas de Episodios.md por firma
//       y crea un PatronDetectado por cluster que cumple el umbral.
//    2. AnalizadorPatrones pasa cada patrón al LLM para que sugiera
//       Nombre, Categoria, RutaScript tentativa y Ejemplo de invocación.
//    3. FrmPatrones muestra tarjetas con "Convertir en Skill" / "Ignorar".
//    4. Al ignorar, la Firma se persiste en PatronesIgnorados.json para
//       no volver a molestar al usuario con el mismo patrón.
//
//  NOTA DE DISEÑO:
//    Firma es la clave estable del patrón — se calcula localmente
//    (sin LLM) y es determinista para que funcione el opt-out.
//    Nombre/Categoria/etc. son sugerencias del LLM, pueden variar
//    entre ejecuciones.
// ============================================================

using System;
using System.Collections.Generic;

namespace OPENGIOAI.Entidades
{
    public class PatronDetectado
    {
        /// <summary>
        /// Firma determinista del patrón (tokens significativos ordenados).
        /// Estable entre ejecuciones — usada como clave para ignorar patrones.
        /// </summary>
        public string Firma { get; set; } = "";

        /// <summary>Nombre sugerido por el LLM. Ej: "Generar reporte semanal".</summary>
        public string NombreSugerido { get; set; } = "";

        /// <summary>Descripción breve de qué hace el patrón.</summary>
        public string Descripcion { get; set; } = "";

        /// <summary>Categoría sugerida: sistema · archivos · ia · web · datos · general.</summary>
        public string Categoria { get; set; } = "general";

        /// <summary>Cuántas veces aparece en Episodios.md.</summary>
        public int Ocurrencias { get; set; } = 0;

        /// <summary>Ejemplos textuales de las líneas originales (hasta 3).</summary>
        public List<string> Ejemplos { get; set; } = new();

        /// <summary>Ejemplo de invocación sugerido. Ej: skill_run("generar_reporte", periodo="semana")</summary>
        public string EjemploInvocacion { get; set; } = "";

        /// <summary>Parámetros inferidos del patrón (lo que varía entre ocurrencias).</summary>
        public List<SkillParametro> ParametrosSugeridos { get; set; } = new();

        /// <summary>Fecha de la detección — útil para mostrar "hace X días" en UI.</summary>
        public DateTime DetectadoEn { get; set; } = DateTime.Now;
    }
}
