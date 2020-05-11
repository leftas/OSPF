using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    public class LinkStateRequestPacket : PacketHeader
    {
        public LinkStateRequestPacket()
        {
            this.Type = PacketType.LKRequest;
        }

        public List<TopologicalDatabasePiece> Requests { get; set; } = new List<TopologicalDatabasePiece>();
    }
}
