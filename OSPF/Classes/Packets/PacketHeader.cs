using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OSPF.Classes.Packets
{
    public enum PacketType
    {
        Error,
        Hello,
        DatabaseDesc,
        LKRequest,
        LKUpdate,
        LKACK,
        Message
    }

    public abstract class PacketHeader
    {
        public int Version { get; set; } = 1;
        public PacketType Type { get; set; }
        public uint PacketLength { get; set; }
        public Router Router { get; set; }
        //Should be area id, but we would not use it.
        public uint Checksum;
        //Should be Autype, but we would not use it.

    }
}