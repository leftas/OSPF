using OSPF.Classes;
using OSPF.Classes.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace OSPF.Classes
{
    public enum NetworkType
    {
        Error,
        Broadcast,
        Multicast,
        Unicast,
        //VirtualLink
    }
    public enum InterfaceState
    {
        Error,
        Down,
        Loopback,
        Waiting,
        P2P,
        DROther,
        Backup,
        DR
    }

    public enum InterfaceEventType
    {
        InterfaceUp,
        WaitTimer,
        BackupSeen,
        NeighborChange,
        LoopInd,
        UnloopInd,
        InterfaceDown
    }

    public class InterfaceEventArgs : EventArgs
    {
        public InterfaceEventType Type { get; set; }
        public InterfaceEventArgs(InterfaceEventType type)
        {
            this.Type = type;
        }
    }

    public class Interface
    {
        public Interface()
        {
            EventOccured += this.Interface_EventOccured;
        }

        private void Interface_EventOccured(object sender, InterfaceEventArgs e)
        {
            switch (e.Type)
            {
                case InterfaceEventType.InterfaceUp:
                    this.helloTimer = new Timer(this.SendHello, null, 0, this.HelloInterval);
                    this.Resender = new Timer(this.ResendPackets, null, this.RxmtInterval, this.RxmtInterval);
                    if (this.ConnectionsTo.Count == 1)
                    {
                        this.State = InterfaceState.P2P;
                    }
                    else if (this.RouterPriority == 0)
                    {
                        this.State = InterfaceState.DROther;
                    }
                    else
                    {
                        this.State = InterfaceState.Waiting;
                        this.waitTimer = new Timer(this.Waiting, null, this.RouterDeadInterval, this.RouterDeadInterval);
                    }
                    break;
                case InterfaceEventType.WaitTimer:
                case InterfaceEventType.BackupSeen:
                case InterfaceEventType.NeighborChange:

                    break;
                case InterfaceEventType.LoopInd:
                    break;
                case InterfaceEventType.UnloopInd:
                    break;
                case InterfaceEventType.InterfaceDown:
                    this.helloTimer?.Dispose();
                    this.Resender?.Dispose();
                    this.Resender = null;
                    this.helloTimer = null;
                    this.Neighbors?.ForEach(x => x.OnNeighborEvent(NeighborEventType.KillNbr));
                    break;
            }
        }

        public event EventHandler<InterfaceEventArgs> EventOccured;
        public void OnEvent(InterfaceEventType type)
        {
            EventOccured?.Invoke(this, new InterfaceEventArgs(type));
        }
        public List<Interface> ConnectionsTo { get; set; } = new List<Interface>();
        public uint InterfaceID { get; set; }
        public NetworkType Type { get; set; }
        public InterfaceState State { get; set; } = InterfaceState.Down;
        public Router Router { get; set; }
        //I used router id, should have been IP interface address and IP interface mask
        //public int AreaID;
        public byte HelloInterval { get; set; }
        public short RouterDeadInterval { get; set; }
        public uint InfTransDelay { get; set; }
        public byte RouterPriority { get; set; }

        private Timer helloTimer;
        private Timer waitTimer;
        private Timer Resender;
        public List<Neighbor> Neighbors { get; set; } = new List<Neighbor>();

        public Router DesignatedRouter { get; set; }
        public Router BackupDesignatedRouter { get; set; }
        public int InterfaceOutputCost { get; set; }
        public uint RxmtInterval { get; set; }

        public void SendHello(object state)
        {
            HelloPacket packet = new HelloPacket
            {
                DeadInt = this.RouterDeadInterval,
                HelloInt = this.HelloInterval,
                RtrPri = this.RouterPriority,
                Router = this.Router,
                DesignatedRouter = this.DesignatedRouter,
                BackupDesignatedRouter = this.BackupDesignatedRouter
            };
            packet.Neighbors = this.Neighbors.Where(x => x.State >= NeighborState.Init).ToList();
            if(state is Interface interf)
            {
                interf.ReceivePacket(this, packet);
            }
            else
            {
                this.ConnectionsTo.ForEach(x => x.ReceivePacket(this, packet));
            }
        }
        private void ResendPackets(object state)
        {

        }
        private void SendDatabasePacket(Interface intfr, DatabaseDescriptionPacket packet)
        {
            intfr.ReceivePacket(this, packet);
        }

        public void ReceivePacket(Interface cameFrom, PacketHeader packet)
        {
            Neighbor neighborFound = null;
            foreach (var neighbor in this.Neighbors)
            {
                if (neighbor.Router == packet.Router)
                {
                    neighborFound = neighbor;
                    break;
                }
            }

            if (packet is HelloPacket helloPacket)
            {
                if(helloPacket.DeadInt != this.RouterDeadInterval || helloPacket.HelloInt != this.HelloInterval)
                {
                    return;
                }
                if (neighborFound == null)
                {
                    neighborFound = new Neighbor(this.RouterDeadInterval)
                    {
                        Router = helloPacket.Router,
                        State = NeighborState.Init,
                        NeighborInterface = cameFrom
                    };
                    lock (this.Router.RouterDatabaseLock)
                    {
                        if (!this.Router.RouterDatabase.ContainsKey(this.Router))
                        {
                            this.Router.RouterDatabase.Add(this.Router, new List<Routing>());
                        }
                    }

                    var route = new Routing
                    {
                        Cost = (uint)new Random((int)DateTime.Today.Ticks).Next(),
                        DestinationRouterID = neighborFound.Router.RouterID,
                        Interface = this,
                        NextHop = helloPacket.Router,
                        LSAge = 0,
                        LSSeqNumber = 1
                    };
                    this.Router.RouterDatabase[this.Router].Add(route);
                    neighborFound.NeighborEvent += this.NeighborFound_NeighborEvent;
                    this.Neighbors.Add(neighborFound);
                }
                neighborFound.OnNeighborEvent(NeighborEventType.HelloReceived);
                bool foundMyself = false;
                foreach(var neighbor in helloPacket.Neighbors)
                {
                    if(neighbor.Router == this.Router)
                    {
                        neighborFound.OnNeighborEvent(NeighborEventType.TwoWayReceived);
                        foundMyself = true;
                    }
                }
                if(!foundMyself)
                {
                    neighborFound.OnNeighborEvent(NeighborEventType.OneWay);
                }
                if(helloPacket.RtrPri != neighborFound.Priority)
                {
                    neighborFound.OnNeighborEvent(NeighborEventType.NeighborChange);
                }
                if(helloPacket.Router == helloPacket.DesignatedRouter && neighborFound.DR != neighborFound.Router || neighborFound.DR == neighborFound.Router && helloPacket.DesignatedRouter != neighborFound.Router)
                {
                    neighborFound.OnNeighborEvent(NeighborEventType.NeighborChange);
                }
                if(helloPacket.Router == helloPacket.BackupDesignatedRouter && neighborFound.BDR != neighborFound.Router || neighborFound.BDR == neighborFound.Router && helloPacket.BackupDesignatedRouter != neighborFound.Router)
                {
                    neighborFound.OnNeighborEvent(NeighborEventType.NeighborChange);
                }
            }
            else if (packet is DatabaseDescriptionPacket dbPacket)
            {
                if(neighborFound == null)
                {
                    return;
                }
                if (neighborFound.State == NeighborState.Down || neighborFound.State == NeighborState.Attempt || neighborFound.State == NeighborState.TwoWay)
                {
                    return;
                }
                else if (neighborFound.State == NeighborState.Init)
                {
                    neighborFound.OnNeighborEvent(NeighborEventType.TwoWayReceived);
                }
                else if (neighborFound.State == NeighborState.ExStart &&
                    ((dbPacket.Flags & DBFLags.Initialize & DBFLags.Master & DBFLags.More) != 0 && dbPacket.DatabasePieces.Count == 0)
                    || ((dbPacket.Flags & (DBFLags.Initialize | DBFLags.Master)) == 0))
                {
                    if (dbPacket.Router.Id > this.Router.Id)
                    {
                        neighborFound.IsMaster = true;
                        neighborFound.SequenceNumber = dbPacket.DDSeqNumber;
                    }
                    else if (dbPacket.Router.Id < this.Router.Id)
                    {
                        neighborFound.IsMaster = false;
                    }
                    neighborFound.OnNeighborEvent(NeighborEventType.NegotiationDone);
                }
                else if (neighborFound.State == NeighborState.Exchange)
                {
                    bool isNeighborMaster = (neighborFound.IsMaster ?? false);
                    bool processFurther = (!isNeighborMaster && dbPacket.DDSeqNumber == neighborFound.SequenceNumber)
                                          || (isNeighborMaster && dbPacket.DDSeqNumber > neighborFound.SequenceNumber);
                    bool discardPacket = !isNeighborMaster && !processFurther;
                    bool repeatPacket = isNeighborMaster && !processFurther;
                    bool generateEvent = (dbPacket.Flags & DBFLags.Initialize) != 0 ||
                                          ((dbPacket.Flags & DBFLags.Master) != 0 && !isNeighborMaster) ||
                                          ((dbPacket.Flags & DBFLags.Master) == 0 && isNeighborMaster) ||
                                            (!discardPacket || !repeatPacket);
                    if (generateEvent)
                    {
                        neighborFound.OnNeighborEvent(NeighborEventType.SeqNumberMismatch);
                    }
                    else if (repeatPacket)
                    {

                    }
                    else if (processFurther)
                    {
                        //For each link state advertisement listed, the router looks in its database to see whether it also has
                        //an instantiation of the link state advertisement.If it does not, or if the database copy is less recent (see Section 13.1),
                        //the link state advertisement is put on the Link state request list so that it can be requested when the neighbor’s state
                        //transitions to Loading

                        //Master Increments the sequence number.If the router has already sent its entire sequence of Database Descriptions,
                        //and the just accepted packet has the more bit(M) set to 0, the neighbor event Exchange Done is generated.
                        //Otherwise, it should send a new Database Description to the slave.

                        //Slave Sets the sequence number to the sequence number appearing in the received packet.The slave must send a
                        //Database Description in reply.If the received packet has the more bit(M) set to 0, and the packet to be sent
                        //by the slave will have the M-bit set to 0 also, the neighbor event Exchange Done is generated.Note that the
                        //slave always generates this event first.


                        foreach (var entry in dbPacket.DatabasePieces)
                        {
                            if (this.Router.RouterDatabase.ContainsKey(entry.Source))
                            {
                                foreach (var destination in (List<Routing>)this.Router.RouterDatabase[entry.Source])
                                {
                                    if (entry.DestinationID == destination.DestinationRouterID)
                                    {
                                        dbPacket.DatabasePieces.Remove(entry);
                                    }
                                }
                            }

                        }
                        var requestPacket = new LinkStateRequestPacket();
                        foreach (var entry in dbPacket.DatabasePieces)
                        {
                            requestPacket.Requests.Add(entry);
                        }
                        requestPacket.Router = this.Router;

                        neighborFound.LinkStateRequests.Add(requestPacket);

                        if (!isNeighborMaster)
                        {
                            DatabaseDescriptionPacket dbpacket = new DatabaseDescriptionPacket();
                            dbpacket.DDSeqNumber = neighborFound.SequenceNumber;
                            dbpacket.LSAge = 1;
                            dbpacket.Router = this.Router;
                            foreach (var info in this.Router.RouterDatabase)
                            {
                                foreach (var dest in info.Value)
                                {
                                    TopologicalDatabasePiece piece = new TopologicalDatabasePiece
                                    {
                                        LSSeqNumber = dest.LSSeqNumber,
                                        LSAge = dest.LSAge,
                                        Source = info.Key,
                                        DestinationID = dest.DestinationRouterID
                                    };
                                    dbpacket.DatabasePieces.Add(piece);
                                }
                            }
                            this.SendDatabasePacket(neighborFound.NeighborInterface, dbpacket);
                        }

                        neighborFound.OnNeighborEvent(NeighborEventType.ExchangeDone);

                    }
                }
                else if (neighborFound.State == NeighborState.Loading || neighborFound.State == NeighborState.Full)
                {

                }
            }
            else if(packet is LinkStateRequestPacket requestPacket)
            {
                if(neighborFound == null)
                {
                    return;
                }

                bool isNeighborMaster = (neighborFound.IsMaster ?? false);
                if (!isNeighborMaster && (neighborFound.State == NeighborState.Exchange ||
                                         neighborFound.State == NeighborState.Loading ||
                                         neighborFound.State == NeighborState.Full))
                {

                }
                else if (isNeighborMaster && (neighborFound.State == NeighborState.Loading ||
                                              neighborFound.State == NeighborState.Full))
                {
                    //Each link state advertisement specified in the Link State Request packet should be located in the router’s database, and
                    //copied into Link State Update packets for transmission to the neighbor.These link state advertisements should NOT
                    //be placed on the Link state retransmission list for the neighbor. If a link state advertisement cannot be found in the
                    //database, something has gone wrong with the synchronization procedure, and neighbor event BadLSReq should be
                    //generated.
                    RouterLinksAdvertismentsPacket adPacket = new RouterLinksAdvertismentsPacket();
                    adPacket.Router = this.Router;
                    foreach(var requestedPacket in requestPacket.Requests)
                    {
                        var databaseEntry = this.Router.RouterDatabase[requestedPacket.Source].FirstOrDefault(x => x.DestinationRouterID == requestedPacket.DestinationID);
                        var linkInfo = new LinkInfo
                        {
                            DestinationID = requestedPacket.DestinationID,
                            Source = requestedPacket.Source,
                            Cost = databaseEntry?.Cost ?? 0,
                            LSSeqNumber = ++databaseEntry.LSSeqNumber
                        };
                        adPacket.Links.Add(linkInfo);
                    }
                    neighborFound.NeighborInterface.ReceivePacket(this, adPacket);

                }
            }
            else if(packet is RouterLinksAdvertismentsPacket updatePacket)
            {
                if (neighborFound == null)
                {
                    return;
                }
                var ackPacket = new LinkStateAckPacket
                {
                    Router = this.Router
                };
                foreach (var link in updatePacket.Links)
                {
                    if(!this.Router.RouterDatabase.ContainsKey(link.Source))
                    {
                        this.Router.RouterDatabase.Add(link.Source, new List<Routing>());
                    }
                    var routingEntry = new Routing
                    {
                        Cost = link.Cost,
                        DestinationRouterID = link.DestinationID,
                        Interface = this,
                        NextHop = updatePacket.Router
                    };
                    this.Router.RouterDatabase[link.Source].Add(routingEntry);
                    ackPacket.DatabasePieces.Add(link);
                }
                neighborFound.NeighborInterface.ReceivePacket(this, ackPacket);
                neighborFound.OnNeighborEvent(NeighborEventType.LoadingDone);
            }
            else if(packet is LinkStateAckPacket ackPacket)
            {
                if (neighborFound == null || neighborFound.State < NeighborState.Exchange)
                {
                    return;
                }
                neighborFound.OnNeighborEvent(NeighborEventType.LoadingDone);

            }
            else if(packet is CustomMessage custom)
            {
                if(custom.RouterDestinationId == this.Router.RouterID)
                {
                    Console.WriteLine($"{this.Router.RouterID}: Got message: {custom.Message} from router({custom.Router.RouterID})");
                }
                else
                {
                    var tableEntry = this.Router.RoutingTable.FirstOrDefault(x => x.DestinationRouterID == custom.RouterDestinationId);
                    Console.WriteLine($"{this.Router.RouterID}: Got message for {custom.RouterDestinationId} sending it to {tableEntry.NextHop.RouterID}");
                    var routerInterface = tableEntry.Interface.ConnectionsTo.Find(x => x.Router == tableEntry.NextHop);
                    routerInterface.ReceivePacket(this, custom);
                }
            }
        }

        private void NeighborFound_NeighborEvent(object sender, NeighborEventArgs e)
        {
            Neighbor neighbor = sender as Neighbor;
            switch (e.Type)
            {
                case NeighborEventType.HelloReceived:
                    neighbor.InactivityTimer.Stop();
                    neighbor.InactivityTimer.Start();
                    if (neighbor.State == NeighborState.Attempt || neighbor.State == NeighborState.Down)
                    {
                        neighbor.State = NeighborState.Init;
                    }
                    break;
                case NeighborEventType.Start when neighbor.State == NeighborState.Down:
                    neighbor.State = NeighborState.Attempt;
                    this.SendHello(neighbor.NeighborInterface);
                    break;
                case NeighborEventType.TwoWayReceived when neighbor.State == NeighborState.Init:
                    neighbor.State = NeighborState.ExStart;
                    this.InitDBPacket(neighbor);
                    break;
                case NeighborEventType.NegotiationDone when neighbor.State == NeighborState.ExStart:
                    // The router must list the contents of its entire area link state database in the neighbor Database summary list.
                    // The area link state database consists of the router links,
                    //network links and summary links contained in the area structure, along with the AS
                    // external links contained in the global structure.AS external link advertisements are
                    //omitted from a virtual neighbor’s Database summary list.Advertisements whose age
                    //is equal to MaxAge are instead added to the neighbor’sLink state retransmission list.
                    //A summary of the Database summary list will be sent to the neighbor in Database
                    //Description packets.Each Database Description Packet has a sequence number, and
                    //is explicitly acknowledged. Only one Database Description Packet is allowed outstanding at any one time. For more detail on the sending and receiving of Database
                    //Description packets, see Sections 10.8 and 10.6.
                    DatabaseDescriptionPacket packet = new DatabaseDescriptionPacket();
                    packet.DDSeqNumber = ++neighbor.SequenceNumber;
                    packet.LSAge = 1;
                    packet.Router = this.Router;
                    foreach(var info in this.Router.RouterDatabase)
                    {
                        foreach(var dest in info.Value)
                        {
                            TopologicalDatabasePiece piece = new TopologicalDatabasePiece
                            {
                                LSSeqNumber = dest.LSSeqNumber,
                                LSAge = dest.LSAge,
                                Source = info.Key,
                                DestinationID = dest.DestinationRouterID
                            };
                            packet.DatabasePieces.Add(piece);
                        }
                    }
                    this.SendDatabasePacket(neighbor.NeighborInterface, packet);
                    neighbor.State = NeighborState.Exchange;
                    break;
                case NeighborEventType.ExchangeDone when neighbor.State == NeighborState.Exchange:
                    neighbor.State = NeighborState.Loading;
                    //Start sending Link State Request packets to the neighbor(see Section 10.9).These
                    //are requests for the neighbor’s more recent advertisements(which were discovered in
                    //the Exchange state).These advertisements are listed in the Link state request list
                    //associated with the neighbor.
                    foreach(var requestPacket in neighbor.LinkStateRequests)
                    {
                        neighbor.NeighborInterface.ReceivePacket(this, requestPacket);
                    }
                    break;
                case NeighborEventType.SeqNumberMismatch:
                    neighbor.State = NeighborState.ExStart;
                    neighbor.LinkStateRequests.Clear();
                    neighbor.LinkStateRetransmissions.Clear();
                    neighbor.DatabaseSummaries.Clear();
                    //Send DB packet
                    break;
                case NeighborEventType.BadLSReq:
                    break;
                case NeighborEventType.LoadingDone when neighbor.State == NeighborState.Loading:
                    this.Router.InvalidateRoutingTable();
                    neighbor.State = NeighborState.Full;
                    break;
                case NeighborEventType.AdjOK:
                    if(neighbor.State == NeighborState.TwoWay)
                    {
                        this.InitDBPacket(neighbor);
                        neighbor.State = NeighborState.Exchange;
                    }
                    else if (neighbor.State >= NeighborState.Exchange && neighbor.NeighborInterface.State == InterfaceState.Down)
                    {
                        neighbor.State = NeighborState.TwoWay;
                        neighbor.LinkStateRequests.Clear();
                        neighbor.LinkStateRetransmissions.Clear();
                        neighbor.DatabaseSummaries.Clear();
                    }

                    break;
                case NeighborEventType.OneWay when neighbor.State >= NeighborState.TwoWay:
                    neighbor.LinkStateRequests.Clear();
                    neighbor.LinkStateRetransmissions.Clear();
                    neighbor.DatabaseSummaries.Clear();
                    neighbor.State = NeighborState.Init;
                    break;
                case NeighborEventType.LLDown:
                case NeighborEventType.KillNbr:
                    neighbor.LinkStateRequests.Clear();
                    neighbor.LinkStateRetransmissions.Clear();
                    neighbor.DatabaseSummaries.Clear();
                    neighbor.InactivityTimer?.Stop();
                    neighbor.State = NeighborState.Down;
                    break;
                case NeighborEventType.InactivityTimer:
                    neighbor.LinkStateRequests.Clear();
                    neighbor.LinkStateRetransmissions.Clear();
                    neighbor.DatabaseSummaries.Clear();
                    break;
                case NeighborEventType.NeighborChange:
                    break;
            }
        }

        private void InitDBPacket(Neighbor neighbor, DBFLags flags = DBFLags.Initialize | DBFLags.Master | DBFLags.More)
        {
            if (neighbor.SequenceNumber == 0)
            {
                neighbor.SequenceNumber = (uint)DateTime.Now.Ticks;
            }
            neighbor.IsMaster = false;
            var dbPacket = new DatabaseDescriptionPacket
            {
                DDSeqNumber = neighbor.SequenceNumber,
                Router = this.Router,
                Flags = flags
            };
            neighbor.DatabaseSummaries.Add(dbPacket);
            this.SendDatabasePacket(neighbor.NeighborInterface, dbPacket);
        }


        public void Waiting(object state)
        {
            this.waitTimer?.Dispose();
            this.waitTimer = null;
            this.OnEvent(InterfaceEventType.WaitTimer);
        }
        public void TurnOn()
        {
            if (this.helloTimer == null && this.ConnectionsTo.Count > 0)
            {
                this.OnEvent(InterfaceEventType.InterfaceUp);
            }
        }

        public void TurnOff()
        {
            this.OnEvent(InterfaceEventType.InterfaceDown);
        }
        //public string AuthenticationKey { get; set;}


    }
}
