using System;
using System.Collections.Generic;
using System.Threading;

namespace CsNet.Dispatch
{
    class ThreadManager<Entity> where Entity : Loopable
    {
        public class EntityInfo
        {
            public Entity target;
            public Thread thread;

            public int TID
            {
                get { return thread.ManagedThreadId; }
            }

            public void Start()
            {
                thread.Start();
            }

            public void Stop()
            {
                target.Stop();
            }

            public void Join()
            {
                thread.Join();
            }
        }
        private Dictionary<int, EntityInfo> m_entities;
        private Func<Entity> m_entityCreater;

        public ThreadManager(Func<Entity> creater)
        {
            m_entities = new Dictionary<int, EntityInfo>();
            m_entityCreater = creater;
        }

        public EntityInfo Create()
        {
            EntityInfo unit = new EntityInfo();
            unit.target = m_entityCreater();
            unit.thread = new Thread(new ThreadStart(unit.target.Run));
            m_entities.Add(unit.thread.ManagedThreadId, unit);
            return unit;
        }

        public void Start(int tid)
        {
            if (m_entities.ContainsKey(tid))
            {
                m_entities[tid].Start();
            }
        }

        public void Start()
        {
            foreach (var unit in m_entities)
            {
                unit.Value.Start();
            }
        }

        public void Stop(int tid)
        {
            if (m_entities.ContainsKey(tid))
            {
                m_entities[tid].Stop();
            }
        }

        public void Stop()
        {
            foreach (var unit in m_entities)
            {
                unit.Value.Stop();
            }
        }

        public void Join(int tid)
        {
            if (m_entities.ContainsKey(tid))
            {
                m_entities[tid].Join();
                m_entities.Remove(tid);
            }
        }

        public void Join()
        {
            foreach (var unit in m_entities)
            {
                unit.Value.Join();
            }
        }

        public void Remove(int tid)
        {
            if (m_entities.ContainsKey(tid))
            {
                m_entities[tid].Stop();
                m_entities[tid].Join();
                m_entities.Remove(tid);
            }
        }

        public void Remove()
        {
            Stop();
            Join();
        }
    }
}
