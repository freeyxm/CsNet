using System;
using System.Collections.Generic;
using System.Threading;

namespace CsNet.Dispatch
{
    /// <summary>
    /// 向Dispatcher派发任务。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Producer<T> : Loopable where T : Task
    {
        private Dispatcher<T> m_dispatcher;

        public Producer(Dispatcher<T> dispatcher)
        {
            m_dispatcher = dispatcher;
        }

        private Producer()
        {
        }

        protected sealed override void Loop()
        {
            Produce();
        }

        /// <summary>
        /// 派发任务
        /// </summary>
        /// <param name="task"></param>
        protected bool Produce(T task)
        {
            return m_dispatcher.Produce(task);
        }

        /// <summary>
        /// 派发任务
        /// </summary>
        /// <param name="tasks"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        protected int Produce(List<T> tasks, int startIndex)
        {
            return m_dispatcher.Produce(tasks, startIndex);
        }

        protected virtual void Produce()
        {
            Thread.Sleep(10); // just sleep a while.
        }
    }
}
