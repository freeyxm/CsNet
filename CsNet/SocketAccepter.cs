using System;
using System.Net.Sockets;
using CsUtil.Util;

namespace CsNet
{
    public class SocketAccepter : SocketHandler
    {
        public delegate void OnAccept(Socket socket);
        private OnAccept m_onAccept;
        private SocketBase m_socket;

        public SocketAccepter(SocketBase socket, SocketListener listener)
            : base(listener)
        {
            m_socket = socket;
            m_socketListener.Register(this, CheckFlag.Read | CheckFlag.Error);
        }

        ~SocketAccepter()
        {
            Dispose();
        }

        public override void Dispose()
        {
            UnRegister();

            base.Dispose();

            if (m_socket != null)
            {
                m_socket.Dispose();
                m_socket = null;
            }

            m_onAccept = null;

            GC.SuppressFinalize(this);
        }

        public void SetOnAcceptSocket(OnAccept onAccept)
        {
            m_onAccept = onAccept;
        }

        public override SocketBase GetSocket()
        {
            return m_socket;
        }

        public override void OnSocketReadReady()
        {
            Socket socket = m_socket.Accept();
            if (socket != null)
            {
                m_onAccept(socket);
            }
            else
            {
                if (m_socket.State != FResult.WouldBlock)
                {
                    Logger.Error(m_socket.ErrorMsg);
                    OnError();
                }
            }
        }

        public override void OnSocketError()
        {
            OnError();
        }

        public override void OnSocketWriteReady()
        {
        }

        private void OnError()
        {
            UnRegister();
        }

        public void UnRegister()
        {
            if (m_socket != null && m_socket.Socket != null)
            {
                m_socketListener.UnRegister(this, CheckFlag.All);
            }
        }
    }
}
