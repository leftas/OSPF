using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    public class LinkStateUpdatePacket : PacketHeader
    {
        public uint NumberOfAds { get; set; }
        public List<RouterLinksAdvertismentsPacket> AdvertismentPackets { get; set; }
    }
}
