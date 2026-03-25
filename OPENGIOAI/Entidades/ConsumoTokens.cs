using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Entidades
{
    public class ConsumoTokens
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }

        public bool Disponible { get; set; }
        public string Proveedor { get; set; }
    }
}
