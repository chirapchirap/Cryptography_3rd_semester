using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLib
{
    public class ChatMessage
    {
        public required string Message { get; set; }
        public required string SenderGuid { get; set; }
        public required DateTime TimeStamp { get; set; }

        public override string ToString()
        {
            return $"({TimeStamp:HH:mm:ss}) [{SenderGuid}]: {Message}";
        }
    }
}
