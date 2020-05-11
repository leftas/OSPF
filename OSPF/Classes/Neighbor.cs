using OSPF.Classes;
using OSPF.Classes.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace OSPF.Classes
{
    public enum NeighborState
    {
        Error,
        Down,
        Attempt,
        Init,
        TwoWay,
        ExStart,
        Exchange,
        Loading,
        Full

    }
    public enum NeighborEventType
    {
        HelloReceived,
        Start,
        TwoWayReceived,
        NegotiationDone,
        ExchangeDone,
        SeqNumberMismatch,
        BadLSReq,
        LoadingDone,
        AdjOK,
        OneWay,
        KillNbr,
        InactivityTimer,
        NeighborChange,
        LLDown
    }

    public class NeighborEventArgs : EventArgs
    {
        public NeighborEventType Type { get; set; }
        public NeighborEventArgs(NeighborEventType type)
        {
            this.Type = type;
        }
    }
    public class Neighbor
    {
        public Neighbor(int deadRouterInterval)
        {
            this.InactivityTimer = new Timer(deadRouterInterval * 1000);
            this.InactivityTimer.Elapsed += this.InactivityTimer_Elapsed;
        }

        private void InactivityTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.OnNeighborEvent(NeighborEventType.InactivityTimer);
        }

        public event EventHandler<NeighborEventArgs> NeighborEvent;

        public void OnNeighborEvent(NeighborEventType type)
        {
            this.NeighborEvent?.Invoke(this, new NeighborEventArgs(type));
        }
        public NeighborState State { get; set; }
        public Timer InactivityTimer { get; }
        public bool? IsMaster { get; set; }
        public uint SequenceNumber { get; set; }
        //public string ID { get; set; }
        public uint Priority { get; set; }

        public Interface NeighborInterface { get; set; }
        public Router Router { get; set; } // Again this is should be IPAddress
        public Router DR { get; set; }
        public Router BDR { get; set; }

        public List<LinkStateAdvertismentHeader> LinkStateRetransmissions { get; set; } = new List<LinkStateAdvertismentHeader>();
        public List<DatabaseDescriptionPacket> DatabaseSummaries { get; set; } = new List<DatabaseDescriptionPacket>();
        public List<LinkStateRequestPacket> LinkStateRequests { get; set; } = new List<LinkStateRequestPacket>();

    }
}
