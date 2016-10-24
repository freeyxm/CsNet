using System;

namespace CsNet.Dispatch
{
    /// <summary>
    /// 从Dispatcher分配并执行任务。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Consumer<T> : Loopable where T : Task
    {
        private Dispatcher<T> m_dispatcher;

        public Consumer(Dispatcher<T> dispatcher)
        {
            m_dispatcher = dispatcher;
        }

        private Consumer()
        {
        }

        protected sealed override void Loop()
        {
            T task;
            if (Consume(out task))
            {
                Execute(task);
            }
        }

        protected bool Consume(out T task)
        {
            return m_dispatcher.Consume(out task);
        }

        protected virtual void Execute(T task)
        {
            if (task != null)
            {
                task.Execute();
            }
        }
    }
}
