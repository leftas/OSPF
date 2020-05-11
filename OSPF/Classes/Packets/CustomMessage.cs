using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    class CustomMessage : PacketHeader
    {
        public CustomMessage()
        {
            this.Type = PacketType.Message;
        }

        public string Message { get; set; }

        public string RouterDestinationId { get; set; }
    }
}
