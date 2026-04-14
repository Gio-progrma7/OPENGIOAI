using System;
using System.Collections.Generic;

namespace OPENGIOAI.Entidades
{
    public enum TipoNodo
    {
        Disparador,
        Condicion,
        Accion,
        Fin
    }

    public enum EstadoNodo { Pendiente, Ejecutando, Completado, Error }

    public class NodoAutomatizacion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TipoNodo TipoNodo { get; set; } = TipoNodo.Accion;
        public string Titulo { get; set; } = "Nodo";
        public string Descripcion { get; set; } = "";
        public string InstruccionNatural { get; set; } = "";
        public string ScriptGenerado { get; set; } = "";
        public string UltimaRespuesta { get; set; } = "";

        /// <summary>Nombre del archivo .py de este nodo (ej: "nodo_01_captura.py")</summary>
        public string NombreScript { get; set; } = "";

        public Dictionary<string, string> Parametros { get; set; } = new();
        public int OrdenEjecucion { get; set; } = 0;
        public int CanvasX { get; set; } = 50;
        public int CanvasY { get; set; } = 50;
        public List<string> ConexionesSalida { get; set; } = new();

        [System.Text.Json.Serialization.JsonIgnore]
        public EstadoNodo Estado { get; set; } = EstadoNodo.Pendiente;
    }
}
