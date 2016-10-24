using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using CsNet;
using CsUtil.Util;

namespace CsNetClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.logLevel = Logger.LogLevel.Debug;

            IPAddress addr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ep = new IPEndPoint(addr, 2016);

            List<Client> clients = new List<Client>();
            List<Thread> threads = new List<Thread>();

            SocketManager socketMgr = new SocketManager(2);
            socketMgr.Start();

            for (int i = 0; i < 200; ++i)
            {
                Client client = new Client(socketMgr);
                Thread thread = new Thread(new ThreadStart(() =>
                {
                    client.Start(ep, 10);
                }));
                clients.Add(client);
                threads.Add(thread);
                thread.Start();
            }

            for (int i = 0; i < threads.Count; ++i)
            {
                threads[i].Join();
            }
            clients.Clear();
            threads.Clear();

            Console.Write("Press any key to quit ...");
            Console.ReadKey();
        }
    }
}
