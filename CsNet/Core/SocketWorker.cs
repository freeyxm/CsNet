using System;
using CsNet.Dispatch;
using CsUtil.Util;

namespace CsNet
{
    class SocketWorker : Consumer<SocketTask>
    {
        public SocketWorker(SocketDispatcher dispatcher)
            : base(dispatcher)
        {
        }

        protected override void Execute(SocketTask task)
        {
            try
            {
                if ((task.check & CheckFlag.Error) != 0)
                {
                    task.handler.OnSocketError();
                }
                else
                {
                    if ((task.check & CheckFlag.Read) != 0)
                        task.handler.OnSocketReadReady();
                    if ((task.check & CheckFlag.Write) != 0)
                        task.handler.OnSocketWriteReady();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
            finally
            {
                task.handler.SetBusy(false);
                task.handler = null;
            }
        }
    }
}
