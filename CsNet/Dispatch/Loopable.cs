using System;

namespace CsNet.Dispatch
{
    public abstract class Loopable : Runnable
    {
        private bool m_stop;

        public Loopable()
        {
        }

        public sealed override void Run()
        {
            SetStop(false);
            Running = true;

            while (!m_stop)
            {
                Loop();
            }

            Running = false;
        }

        protected abstract void Loop();

        public void Stop()
        {
            SetStop(true);
        }

        protected void SetStop(bool stop)
        {
            m_stop = stop;
        }
    }
}
