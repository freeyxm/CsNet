using System;

namespace CsNet.Dispatch
{
    public interface IRunnable
    {
        void Run();
    }

    public abstract class Runnable : IRunnable
    {
        private bool m_running;

        public Runnable()
        {
            m_running = false;
        }

        public abstract void Run();

        public bool Running
        {
            get { return m_running; }
            protected set { m_running = value; }
        }
    }
}
