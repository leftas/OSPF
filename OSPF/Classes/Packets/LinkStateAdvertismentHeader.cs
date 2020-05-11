using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    public enum LSType
    {
        Error,
        RouterLinks,
        NetworkLinks,
        SummaryLinkIP,
        SummaryLinkASBR,
        ASExternalLink
    }
    public abstract class LinkStateAdvertismentHeader : PacketHeader
    {
        protected LinkStateAdvertismentHeader()
        {
            this.Type = PacketType.LKUpdate;
        }
        public short LSAge { get; set; }
        public uint LSSequenceNumber { get; set; }

        //public short LSChecksum { get; set; }
        //public short Length { get; set; }
    }
}
