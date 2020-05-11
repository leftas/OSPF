using OSPF.Classes.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;

namespace OSPF.Classes
{
    public class Router : IEquatable<Router>
    {
        public Router(string routerID)
        {
            this.RouterID = routerID;
            var number = GetDigits(routerID);
            if(number == 0)
            {
                number = new Random(123).Next();
            }
            this.Id = (uint)number;
        }

        public int GetDigits(string @string)
        {
            string b = string.Empty;
            int val = 0;

            for (int i = 0; i < @string.Length; i++)
            {
                if (char.IsDigit(@string[i]))
                {
                    b += @string[i];
                }
            }

            if (b.Length > 0)
            {
                val = int.Parse(b);
            }
            return val;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Router);
        }
        public bool Equals(Router other)
        {
            return other != null && this.RouterID == other.RouterID;
        }

        public byte HelloInterval { get; set; } = 10;
        public short RouterDeadInterval { get; set; } = 4 * 10;
        public uint InfTransDelay { get; set; }
        public byte RouterPriority { get; set; }
        public List<Routing> RoutingTable { get; set; } = new List<Routing>();

        public object RouterDatabaseLock = new object();

        public Dictionary<Router, List<Routing>> RouterDatabase = new Dictionary<Router, List<Routing>>();

        public bool AddLink(Router anotherRouter, Interface interfaceFrom = null, Interface interfaceTo = null)
        {
            Interface intfr1 = interfaceFrom,
                      intfr2 = interfaceTo;
            if (interfaceFrom == null)
            {
                intfr1 = new Interface()
                {
                    InterfaceID = (uint)this.Interfaces.Count + 1,
                    Router = this,
                    HelloInterval = this.HelloInterval,
                    RouterDeadInterval = this.RouterDeadInterval,
                    InfTransDelay = this.InfTransDelay,
                    RouterPriority = this.RouterPriority
                };
                this.Interfaces.Add(intfr1);
            }
            if (interfaceTo == null)
            {
                intfr2 = new Interface()
                {
                    InterfaceID = (uint)anotherRouter.Interfaces.Count + 1,
                    Router = anotherRouter,
                    HelloInterval = anotherRouter.HelloInterval,
                    RouterDeadInterval = anotherRouter.RouterDeadInterval,
                    InfTransDelay = anotherRouter.InfTransDelay,
                    RouterPriority = anotherRouter.RouterPriority
                };
                anotherRouter.Interfaces.Add(intfr2);
            }

            if(!intfr1.ConnectionsTo.Contains(intfr2) && !intfr2.ConnectionsTo.Contains(intfr1))
            {
                intfr1.ConnectionsTo.Add(intfr2);
                intfr2.ConnectionsTo.Add(intfr1);
                intfr1.TurnOn();
                intfr2.TurnOn();
            }
            else
            {
                if(intfr1 != interfaceFrom)
                {
                    this.Interfaces.RemoveAt(this.Interfaces.Count - 1);
                }
                if(intfr2 != interfaceTo)
                {
                    anotherRouter.Interfaces.RemoveAt(this.Interfaces.Count - 1);
                }

                return false;
            }


            return true;
        }

        public bool RemoveLink(Router router)
        {
            foreach(var @interface in this.Interfaces)
            {
                foreach(var connectionsTo in @interface.ConnectionsTo)
                {
                    if(connectionsTo.Router == router)
                    {
                        connectionsTo.TurnOff();
                        @interface.TurnOff();
                        return true;
                    }
                }

            }
            return false;
        }

        public bool SendMessage(string DestinationId, string message)
        {
            foreach (var route in RoutingTable)
            {
                if (DestinationId == route.DestinationRouterID)
                {
                    CustomMessage customMessage = new CustomMessage
                    {
                        Router = this,
                        Message = message,
                        RouterDestinationId = DestinationId
                    };
                }
            }
            return false;
        }
        public string RouterID { get; set; }
        public uint Id { get; set; }
        // Area structs - NO
        // Backbone struct - No
        // Virtual links - NO
        // External routes - No
        // External link ads - NO
        // Routing table
        public void InvalidateRoutingTable()
        {
            foreach(var router in RouterDatabase)
            {
                this.DatabaseRoutes.AddVertex(router.Key);
                foreach(var destination in router.Value)
                {
                    if (!this.DatabaseRoutes.ContainsVertex(destination.NextHop))
                    {
                        var edge = new Edge<Router>(router.Key, destination.NextHop);
                        this.DatabaseRoutes.AddEdge(edge);
                        this.Costs.Add(edge, (uint)new Random((int)DateTime.Now.Ticks).Next());
                    }
                }
            }
            ComputeRoutingTable(this);
        }

        private void ComputeRoutingTable(Router @from)
        {
            var edgeCost = AlgorithmExtensions.GetIndexer(Costs);
            var tryGetPath = DatabaseRoutes.ShortestPathsDijkstra(edgeCost, @from);

            foreach (var vertex in this.DatabaseRoutes.Vertices)
            {
                if (tryGetPath(vertex, out IEnumerable<Edge<Router>> path))
                {
                    var routingEntry = new Routing
                    {
                        Cost = (uint)edgeCost(path.First()),
                        DestinationRouterID = path.Last().Target.RouterID,
                        Interface = this.Interfaces.Find(x => x.Router == path.First().Target),
                        LSAge = 0,
                        LSSeqNumber = 0
                    };
                    this.RoutingTable.Add(routingEntry);
                }
                else
                {
                    Console.WriteLine("No path found from {0} to {1}.");
                }
            }
        }

        public List<Interface> Interfaces { get; set; } = new List<Interface>();
        public QuickGraph.AdjacencyGraph<Router, Edge<Router>> DatabaseRoutes { get; set; } = new AdjacencyGraph<Router, Edge<Router>>();
        public Dictionary<Edge<Router>, double> Costs { get; set; } = new Dictionary<Edge<Router>, double>();

        public override int GetHashCode()
        {
            int sum = 0;
            unchecked
            {
                sum += this.RouterID.GetHashCode();
                sum += this.Id.GetHashCode();
            }
            return sum;
        }
    }
}
