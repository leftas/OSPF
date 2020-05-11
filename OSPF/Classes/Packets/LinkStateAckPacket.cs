using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    public class LinkStateAckPacket : PacketHeader
    {
        public LinkStateAckPacket()
        {
            this.Type = PacketType.LKACK;
        }

        public List<TopologicalDatabasePiece> DatabasePieces { get; set; } = new List<TopologicalDatabasePiece>();
    }
}
