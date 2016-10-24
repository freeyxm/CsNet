using System;

namespace CsNet.Dispatch
{
    public class Manager<T> where T : Task
    {
        private Dispatcher<T> m_dispatcher;
        private MasterMgr m_masterMgr;
        private WorkerMgr m_workerMgr;

        public Manager(int capacity, int initSize)
        {
            m_dispatcher = CreateDispatcher(capacity, initSize);
            m_masterMgr = new MasterMgr(CreateProducer);
            m_workerMgr = new WorkerMgr(CreateConsumer);
        }

        public void Start()
        {
            m_workerMgr.Start();
            m_masterMgr.Start();
        }

        public void Stop()
        {
            m_masterMgr.Stop();
            m_workerMgr.Stop();
        }

        public void Join()
        {
            m_masterMgr.Join();
            m_workerMgr.Join();
        }

        #region Master
        public void CreateMasters(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                CreateMaster();
            }
        }

        public int CreateMaster()
        {
            var master = m_masterMgr.Create();
            return master.TID;
        }

        public void StartMaster(int tid)
        {
            m_masterMgr.Start(tid);
        }

        public void StopMaster(int tid)
        {
            m_masterMgr.Stop(tid);
        }

        public void JoinMaster(int tid)
        {
            m_masterMgr.Join(tid);
        }

        public void RemoveMaster(int tid)
        {
            m_masterMgr.Remove(tid);
        }
        #endregion

        #region Worker
        public void CreateWorkers(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                CreateWorker();
            }
        }

        public int CreateWorker()
        {
            var worker = m_workerMgr.Create();
            return worker.TID;
        }

        public void StartWorker(int tid)
        {
            m_workerMgr.Start(tid);
        }

        public void StopWorker(int tid)
        {
            m_workerMgr.Stop(tid);
        }

        public void JoinWorker(int tid)
        {
            m_workerMgr.Join(tid);
        }

        public void RemoveWorker(int tid)
        {
            m_workerMgr.Remove(tid);
        }
        #endregion

        public void SetProducerTimeout(int milliseconds)
        {
            m_dispatcher.SetProducerTimeout(milliseconds);
        }

        public void SetConsumerTimeout(int milliseconds)
        {
            m_dispatcher.SetConsumerTimeout(milliseconds);
        }

        public Dispatcher<T> GetDispatcher()
        {
            return m_dispatcher;
        }

        protected virtual Dispatcher<T> CreateDispatcher(int capacity, int initSize)
        {
            return new Dispatcher<T>(capacity, initSize);
        }

        protected virtual Producer<T> CreateProducer()
        {
            return new Producer<T>(m_dispatcher);
        }

        protected virtual Consumer<T> CreateConsumer()
        {
            return new Consumer<T>(m_dispatcher);
        }

        private class MasterMgr : ThreadManager<Producer<T>>
        {
            public MasterMgr(Func<Producer<T>> creater)
                : base(creater)
            {
            }
        }

        private class WorkerMgr : ThreadManager<Consumer<T>>
        {
            public WorkerMgr(Func<Consumer<T>> creater)
                : base(creater)
            {
            }
        }
    }
}
