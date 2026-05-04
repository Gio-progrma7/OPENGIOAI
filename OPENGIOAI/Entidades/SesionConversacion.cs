using System;
using System.Collections.Generic;
using System.Linq;

namespace OPENGIOAI.Entidades
{
    /// <summary>Un turno de conversación: pregunta del usuario + respuesta del agente.</summary>
    public class TurnoChat
    {
        public string Instruccion    { get; set; } = "";
        public string Respuesta      { get; set; } = "";
        public DateTime Timestamp    { get; set; } = DateTime.Now;
        public int TokensEstimados   { get; set; }
    }

    /// <summary>Sesión completa de chat — persiste en un archivo JSON individual.</summary>
    public class SesionConversacion
    {
        public string SesionId       { get; set; } = "";
        public string Titulo         { get; set; } = "";
        public string Modelo         { get; set; } = "";
        public string Proveedor      { get; set; } = "";
        public DateTime Inicio       { get; set; } = DateTime.Now;
        public DateTime? Fin         { get; set; }
        public List<TurnoChat> Turnos { get; set; } = new();

        public int TotalTurnos    => Turnos.Count;
        public int TotalTokens    => Turnos.Sum(t => t.TokensEstimados);
        public TimeSpan Duracion  => (Fin ?? DateTime.Now) - Inicio;
    }

    /// <summary>Entrada en el índice diario — lo mínimo para pintar la lista.</summary>
    public class SesionIndiceItem
    {
        public string SesionId    { get; set; } = "";
        public string Titulo      { get; set; } = "";
        public string Modelo      { get; set; } = "";
        public string Proveedor   { get; set; } = "";
        public DateTime Inicio    { get; set; }
        public DateTime? Fin      { get; set; }
        public int TotalTurnos    { get; set; }
        public int TotalTokens    { get; set; }
    }
}
