using OSPF.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace OSPF
{
    static class Program
    {
        static List<Router> Routers = new List<Router>();
        static void Main(string[] args)
        {
            var printStr = "OPTIONS: \n" +
                 "   [1] View current network. \n" +
                 "   [2] Add router. \n" +
                 "   [3] Remove router. \n" +
                 "   [4] Add link. \n" +
                 "   [5] Remove link \n" +
                 "   [6] Send packet. \n" +
                 "   [7] Load config \n" +
                 "   [0] Exit. \n";

            Console.WriteLine(printStr + "\n");
            int option;
            while (true)
            {
                Console.WriteLine("Enter option number (0 - 8): ");
                string optionStr = Console.ReadLine();
                try
                {
                    option = int.Parse(optionStr);
                }
                catch (FormatException e)
                {
                    Console.WriteLine("Input is invalid.");
                    continue;
                }
                if (option < 0 || option > 8)
                {
                    Console.WriteLine("Option number must be from 1 to 7 inclusive.");
                }

                switch (option)
                {
                    case 1:
                        Console.WriteLine("Which Router?:");
                        string router = Console.ReadLine();
                        var foundRouter = Routers.Find(x => x.RouterID == router);
                        if(foundRouter == null)
                        {
                            Console.WriteLine("Such a router was not found!");
                            break;
                        }
                        Console.WriteLine("Destination Router ID\\Cost\\NextHop\\Interface\\LSAge");
                        foreach(var links in foundRouter.RouterDatabase[foundRouter])
                        {
                            Console.WriteLine($"{links.DestinationRouterID} {links.Cost} {links.NextHop.RouterID} {links.Interface} {links.LSAge}");
                        }
                        break;
                    case 2:
                        Console.WriteLine("Router name:");
                        var routerName = Console.ReadLine();
                        var newRouter = new Router(routerName);
                        Routers.Add(newRouter);
                        break;
                    case 3:
                        Console.WriteLine("Router name:");
                        var routerName1 = Console.ReadLine();
                        var foundedRouter2 = Routers.Find(x => x.RouterID == routerName1);
                        if (foundedRouter2 == null)
                        {
                            Console.WriteLine("Router was not found!");
                            break;
                        }
                        foreach (var interfaces in foundedRouter2.Interfaces)
                        {
                            interfaces.TurnOff();
                        }
                        break;
                    case 4:
                        Console.WriteLine("Router name from:");
                        var fromRouter = Console.ReadLine();
                        var fromFoundedRouter = Routers.Find(x => x.RouterID == fromRouter);
                        if (fromFoundedRouter == null)
                        {
                            Console.WriteLine("Router was not found!");
                            break;
                        }
                        Console.WriteLine("Router name to:");
                        var toRouter = Console.ReadLine();
                        var toFoundedRouter = Routers.Find(x => x.RouterID == toRouter);
                        if (toFoundedRouter == null)
                        {
                            Console.WriteLine("Router was not found!");
                            break;
                        }
                        if(fromFoundedRouter.AddLink(toFoundedRouter))
                        {

                        }

                        break;
                    case 5:
                        Console.WriteLine("Router name from:");
                        var fromRouterRemove = Console.ReadLine();
                        var fromFoundedRouterRemove = Routers.Find(x => x.RouterID == fromRouterRemove);
                        if(fromFoundedRouterRemove == null)
                        {
                            Console.WriteLine("Router was not found!");
                            break;
                        }
                        Console.WriteLine("Router name to:");
                        var toRouterRemove = Console.ReadLine();
                        var toFoundedRouterRemove = Routers.Find(x => x.RouterID == toRouterRemove);
                        if (toFoundedRouterRemove == null)
                        {
                            Console.WriteLine("Router was not found!");
                            break;
                        }
                        fromFoundedRouterRemove.RemoveLink(toFoundedRouterRemove);
                        break;
                    case 6:
                        Console.WriteLine("Router name from:");
                        var sendMessageFrom = Console.ReadLine();
                        var sendMessageFromRouter = Routers.Find(x => x.RouterID == sendMessageFrom);
                        if (sendMessageFromRouter == null)
                        {
                            Console.WriteLine("Router was not found!");
                            break;
                        }
                        Console.WriteLine("Router name to:");
                        var sendMessageTo = Console.ReadLine();
                        Console.WriteLine("Enter message:");
                        if(!sendMessageFromRouter.SendMessage(sendMessageTo, Console.ReadLine()))
                        {
                            Console.WriteLine("Could not find destination!");
                        }
                        break;
                    case 7:
                        for(int i = 1; i < 5; i++)
                        {
                            var routerConfig = new Router($"R0{i}");
                            Routers.Add(routerConfig);
                        }
                        Routers[0].AddLink(Routers[1]);
                        Routers[0].AddLink(Routers[2]);
                        Routers[1].AddLink(Routers[3]);
                        Routers[2].AddLink(Routers[3]);
                        break;
                    case 0:
                        Console.WriteLine("Exit!");
                        System.Environment.Exit(0);
                        break;
                }
            }
        }
    }
}
