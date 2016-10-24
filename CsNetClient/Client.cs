using System;
using System.Collections.Generic;
using CsNet;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CsNet.Util;

namespace CsNetClient
{
    class Client
    {
        private SocketManager m_socketMgr;
        private SocketBase m_socket;
        private bool m_bRun;
        private bool m_bReconnect;
        private int m_reconnectCount;
        private static int m_sendCount = 0;
        private static int m_recvCount = 0;

        public Client(SocketManager mgr)
        {
            m_socket = new SocketTcp(AddressFamily.InterNetwork);
            m_socketMgr = mgr;
        }

        public void Start(EndPoint ep, int sleep)
        {
            var ret = m_socket.Connect(ep);
            if (ret != FResult.Success)
            {
                Logger.Error("Connect error: {0}", m_socket.ErrorMsg);
                return;
            }

            string msg = "Hi server!";
            byte[] bytes = Encoding.UTF8.GetBytes(msg);

            m_bRun = true;
            m_bReconnect = false;
            m_reconnectCount = 0;

            var socket = new SocketMsg(m_socket, m_socketMgr.GetSocketListener());
            socket.SetOnSocketError(OnSocketError);
            socket.SetOnRecvedData(OnRecvedData);
            socket.Register();

            while (m_bRun)
            {
                if (m_bReconnect)
                {
                    ++m_reconnectCount;
                    if (socket.GetSocket().Reconnect() == FResult.Success)
                    {
                        Logger.Info("Reconnect Success.");
                        socket.Register();
                        m_bReconnect = false;
                    }
                    else
                    {
                        Logger.Info("Reconnect failed ({0}): {1}", m_reconnectCount, socket.GetSocket().ErrorMsg);
                        if (m_reconnectCount >= 5)
                        {
                            m_bRun = false;
                            break;
                        }
                        Thread.Sleep(2000);
                    }
                    continue;
                }

                socket.SendMsg(bytes, () =>
                {
                    ++m_sendCount;
                    //Logger.Debug("Send finished.");
                }, () =>
                {
                    Logger.Debug("Send error.");
                });

                if (sleep > 0)
                    Thread.Sleep(sleep);
                else
                    Console.ReadLine();
            }
        }

        void OnRecvedData(SocketMsg socket, byte[] data)
        {
            ++m_recvCount;
            if (m_recvCount % 1000 == 0)
            {
                Logger.Debug("Send: {0}K, Recv: {1}K", m_sendCount / 1000, m_recvCount / 1000);
            }

            string msg = Encoding.UTF8.GetString(data);
            //Logger.Debug(string.Format("Recv msg: {0}", msg));
        }

        void OnSocketError(SocketMsg socket)
        {
            m_bReconnect = true;
        }
    }
}
