using System;
using CsNet.Dispatch;

namespace CsNet
{
    public class SocketManager : Manager<SocketTask>
    {
        private SocketDispatcher m_dispatcher;
        private SocketListener m_listener;

        public SocketManager(int workerNum)
            : base(1000, 100)
        {
            InitDispatcher();
            CreateMaster();
            CreateWorkers(workerNum);
        }

        public SocketListener GetSocketListener()
        {
            return m_listener;
        }

        private void InitDispatcher()
        {
            m_dispatcher = GetDispatcher() as SocketDispatcher;
            m_dispatcher.SetProducerTimeout(1000);
            m_dispatcher.SetConsumerTimeout(500);
        }

        protected override Dispatcher<SocketTask> CreateDispatcher(int capacity, int initSize)
        {
            return new SocketDispatcher(capacity, initSize);
        }

        protected override Producer<SocketTask> CreateProducer()
        {
            SocketListener listener = new SocketListener(1, 10000);
            if (m_listener == null)
            {
                m_listener = listener;
            }
            return new SocketMaster(m_dispatcher, listener);
        }

        protected override Consumer<SocketTask> CreateConsumer()
        {
            return new SocketWorker(m_dispatcher);
        }
    }
}
