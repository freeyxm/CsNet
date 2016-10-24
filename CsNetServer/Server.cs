using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CsNet;
using CsNet.Util;

namespace CsNetServer
{
    class Server
    {
        private SocketManager m_socketMgr;
        private SocketBase m_socket;
        private SocketAccepter m_socketAccepter;
        private List<SocketMsg> m_clients;
        private int m_requestCount;
        private int m_responseCount;
        private byte[] m_debugData;

        public Server()
        {
            m_socket = new SocketTcp(AddressFamily.InterNetwork);
            m_clients = new List<SocketMsg>();
            m_requestCount = 0;
            m_responseCount = 0;

            m_debugData = Encoding.UTF8.GetBytes(string.Format("hi, this is debug data."));
        }

        public void Start(EndPoint ep)
        {
            var ret = m_socket.Bind(ep);
            if (ret != FResult.Success)
            {
                Logger.Error("Bind error: {0}", m_socket.ErrorMsg);
                return;
            }

            ret = m_socket.Listen(100);
            if (ret != FResult.Success)
            {
                Logger.Error("Listen error: {0}", m_socket.ErrorMsg);
                return;
            }

            m_socketMgr = new SocketManager(2);
            m_socketMgr.Start();

            m_socketAccepter = new SocketAccepter(m_socket, m_socketMgr.GetSocketListener());
            m_socketAccepter.SetOnAcceptSocket(OnAcceptSocket);

            Logger.Info("Server started: {0}", m_socket.Socket.LocalEndPoint.ToString());

            while (true)
            {
                Console.Write(">");
                string cmd = Console.ReadLine();
                if (cmd == "quit")
                {
                    Logger.Info("Stopping server ...");
                    m_socketMgr.Stop();
                    break;
                }
                else if (cmd == "client")
                {
                    Console.WriteLine(m_clients.Count);
                }
                else if (cmd == "request")
                {
                    Console.WriteLine(m_requestCount);
                }
                else if(cmd == "debug on")
                {
                    Logger.LogLevel = Logger.Level.Debug;
                }
                else if (cmd == "debug off")
                {
                    Logger.LogLevel = Logger.Level.Info;
                }
            }

            Logger.Info("Waiting server stop ...");
            m_socketMgr.Join();
            Logger.Info("Server Stopped.");

            Console.Write("Press any key to quit ...");
            Console.ReadKey();
        }

        void OnAcceptSocket(Socket socket)
        {
            SocketMsg s = new SocketMsg(new SocketBase(socket), m_socketMgr.GetSocketListener());
            s.SetOnRecvedData(OnRecvedData);
            s.SetOnSocketError(OnSocketError);
            s.Register();
            lock (m_clients)
            {
                m_clients.Add(s);
                Logger.Debug("Client ON, count: {0}", m_clients.Count);
            }
        }

        void OnRecvedData(SocketMsg socket, byte[] data)
        {
            ++m_requestCount;
            if (m_requestCount % 1000 == 0)
            {
                Logger.Debug("Request : {0}K, Response: {1}K", m_requestCount / 1000, m_responseCount / 1000);
            }

            string msg = Encoding.UTF8.GetString(data);
            string addr = socket.GetSocket().Socket.RemoteEndPoint.ToString();
            //Logger.Debug("Recv msg: {0}, addr: {1}", msg, addr);

            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format("hi {0}, {1}", addr, time));

            socket.SendMsg(bytes, () => { ++m_responseCount; }, null);
        }

        void OnSocketError(SocketMsg socket)
        {
            Close(socket);
        }

        void Close(SocketMsg socket)
        {
            socket.Dispose();
            lock (m_clients)
            {
                m_clients.Remove(socket);
                Logger.Debug("Client OFF, count: {0}", m_clients.Count);
            }
        }
    }
}
