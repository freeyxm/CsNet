using System;
using System.Net;
using CsUtil.Util;

namespace CsNetServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.logLevel = Logger.LogLevel.Info;

            IPEndPoint ep = new IPEndPoint(0, 2016);
            Server server = new Server();
            server.Start(ep);
        }
    }
}
