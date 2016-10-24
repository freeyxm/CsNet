using System;
using System.Collections.Generic;
using CsNet.Dispatch;

namespace CsNet
{
    class SocketMaster : Producer<SocketTask>
    {
        private SocketDispatcher m_dispatcher;
        private SocketListener m_listener;

        public SocketMaster(SocketDispatcher dispatcher, SocketListener listener)
            : base(dispatcher)
        {
            m_dispatcher = dispatcher;
            m_listener = listener;
            m_listener.SetDispatch(m_dispatcher.Dispatch);
        }

        protected override void Produce()
        {
            m_listener.Listen();
        }

        public SocketListener GetListener()
        {
            return m_listener;
        }
    }
}
