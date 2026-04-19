// ============================================================
//  Habilidad.cs
//  Capacidad cognitiva del agente (memoria, patrones, etc.)
//  que el usuario puede activar o desactivar desde FrmHabilidades.
//
//  DIFERENCIA CON Skill.cs:
//    - Skill (existente): plugin externo en Python que extiende
//      QUÉ PUEDE HACER el agente (listar facturas, enviar mails...).
//    - Habilidad (este): toggle de comportamiento interno que
//      controla CÓMO PROCESA el agente (recordar, detectar
//      patrones, sugerir...). Vive en el orquestador, no en Python.
// ============================================================

namespace OPENGIOAI.Entidades
{
    public class Habilidad
    {
        /// <summary>Clave estable usada desde el código (ej: "memoria").</summary>
        public string Clave { get; set; } = "";

        /// <summary>Nombre visible en FrmHabilidades.</summary>
        public string Nombre { get; set; } = "";

        /// <summary>Icono de una sola emoji/glifo para la tarjeta.</summary>
        public string Icono { get; set; } = "🧠";

        /// <summary>Descripción corta del efecto que tiene al activarla.</summary>
        public string Descripcion { get; set; } = "";

        /// <summary>
        /// Efecto en el consumo de tokens, como etiqueta visible
        /// para que el usuario sepa qué prende/apaga al togglearla.
        /// </summary>
        public string ImpactoTokens { get; set; } = "";

        /// <summary>True = la capacidad está encendida.</summary>
        public bool Activa { get; set; } = false;
    }
}
