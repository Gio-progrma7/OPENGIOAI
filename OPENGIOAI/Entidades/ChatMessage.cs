using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Entidades
{
    // Clase para representar un mensaje en el chat
    public class ChatMessage
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
