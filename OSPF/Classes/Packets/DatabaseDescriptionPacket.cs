using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes.Packets
{
    public class TopologicalDatabasePiece
    {
        public Router Source { get; set; }
        public string DestinationID { get; set; }

        public int LSSeqNumber { get; set; }

        public uint LSAge { get; set; }

    }
    [Flags]
    public enum DBFLags
    {
        None = 0,
        Initialize = 1,
        More = 2,
        Master = 4
    }
    public class DatabaseDescriptionPacket : PacketHeader
    {
        public DatabaseDescriptionPacket()
        {
            this.Type = PacketType.DatabaseDesc;
        }

        public DBFLags Flags { get; set; }

        public uint DDSeqNumber { get; set; }

        public short LSAge { get; set; }

        public List<TopologicalDatabasePiece> DatabasePieces { get; set; } = new List<TopologicalDatabasePiece>();
    }
}
