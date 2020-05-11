using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    public class LinkInfo : TopologicalDatabasePiece
    {
        public uint Cost { get; set; }

    }
    public class RouterLinksAdvertismentsPacket : LinkStateAdvertismentHeader
    {
        public List<LinkInfo> Links { get; set; } = new List<LinkInfo>();

    }
}
