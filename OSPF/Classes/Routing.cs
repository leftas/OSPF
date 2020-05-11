using System;
using System.Collections.Generic;
using System.Text;

namespace OSPF.Classes
{
    public class Routing
    {
        public string DestinationRouterID { get; set; }
        //public byte TOS { get; set; }
        //public uint Area { get; set; }
        public uint Cost { get; set; }

        public Interface Interface { get; set; }

        public Router NextHop { get; set; }

        public uint LSAge { get; set; }

        public int LSSeqNumber { get; set; }

        //public Router AdvertisingRouter { get; set; }
    }
}
