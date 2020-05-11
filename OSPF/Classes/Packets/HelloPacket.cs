using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OSPF.Classes.Packets
{
    public class HelloPacket : PacketHeader
    {
        public HelloPacket()
        {
            this.Type = PacketType.Hello;
        }
        //public IPAddress NetworkMask { get; set; } We won't implement that.
        public short DeadInt { get; set; }
        public byte HelloInt { get; set; }
        /// <summary>
        /// Router priority
        /// </summary>
        public byte RtrPri { get; set; }
        public Router DesignatedRouter { get; set; }
        public Router BackupDesignatedRouter { get; set; }
        public List<Neighbor> Neighbors { get; set; } = new List<Neighbor>();
    }
}
