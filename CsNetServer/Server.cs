using System;
using System.Collections.Generic;
using CsNet;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CsNetServer
{
    class Server
    {
        private SocketBase m_socket;
        private List<MsgManager> m_clients;
        private int m_requestCount;

        public Server()
        {
            m_socket = new SocketTcp(AddressFamily.InterNetwork);
            m_clients = new List<MsgManager>();
            m_requestCount = 0;
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

            Thread listner = new Thread(new ThreadStart(SocketListener.Instance.Run));
            listner.Start();

            Logger.Info("Server started: {0}", m_socket.GetSocket().LocalEndPoint.ToString());

            while (true)
            {
                Socket socket = m_socket.Accept();
                if (socket != null)
                {
                    MsgManager mgr = new MsgManager(new SocketBase(socket));
                    mgr.SetOnRecvedData(OnRecvedData);
                    mgr.SetOnSocketError(OnSocketError);
                    mgr.Register();
                    m_clients.Add(mgr);
                    Logger.Info("Client ON, count: {0}", m_clients.Count);
                }
            }
        }

        void OnRecvedData(MsgManager mgr, byte[] data)
        {
            ++m_requestCount;
            if (m_requestCount % 1000 == 0)
            {
                Logger.Info("Request count: {0}K", m_requestCount / 1000);
            }

            string msg = Encoding.UTF8.GetString(data);
            string addr = mgr.GetSocket().RemoteEndPoint.ToString();
            Logger.Debug("Recv msg: {0}, addr: {1}", msg, addr);

            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            byte[] bytes = Encoding.UTF8.GetBytes(string.Format("hi {0}, {1}", addr, time));
            mgr.SendMsg(bytes, null, () =>
            {
                Close(mgr);
            });
        }

        void OnSocketError(MsgManager mgr)
        {
            Close(mgr);
        }

        void Close(MsgManager mgr)
        {
            m_clients.Remove(mgr);
            Logger.Info("Client OFF, count: {0}", m_clients.Count);
        }
    }
}
