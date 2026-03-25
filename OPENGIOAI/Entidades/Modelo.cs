using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Entidades
{
    public class Modelo
    {
       public Servicios Agente {get; set;}
       public string ApiKey { get; set; }
       public bool Estado { get; set; }
       public string Modelos { get; set; }    

    }
}
