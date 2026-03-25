using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENGIOAI.Entidades
{
    public class SlackMessage
    {
        public string Text { get; set; }
        public string User { get; set; }
        public string BotId { get; set; }
        public string Timestamp { get; set; }
    }
}