using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Entidades
{
    public class ConfiguracionIA
    {
        public string Instruccion { get; set; }
        public string Modelo { get; set; }
        public string RutaArchivo { get; set; }
        public string ApiKey { get; set; }
        public string ClavesKeyConfiguradas { get; set; }
        public bool Chat { get; set; }
        public Servicios Servicio { get; set; }
    }
}
