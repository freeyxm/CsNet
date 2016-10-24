using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using CsUtil.Util;

namespace CsNet
{
    public enum CheckFlag
    {
        Read = 0x01 << 0,
        Write = 0x01 << 1,
        Error = 0x01 << 2,

        RW = Read | Write,
        All = Read | Write | Error,
    }

    /// <summary>
    /// 监听Socket状态
    /// </summary>
    public class SocketListener
    {
        private class CheckInfo
        {
            public Dictionary<Socket, SocketHandler> waitSockets;
            public List<Socket> checkSockets;
            public CheckFlag checkFlag;
            public SpinLock spinLock;

            public CheckInfo(int capacity, CheckFlag flag)
            {
                waitSockets = new Dictionary<Socket, SocketHandler>(capacity);
                checkSockets = new List<Socket>(capacity);
                checkFlag = flag;
                spinLock = new SpinLock();
            }

            private CheckInfo() { }
        }
        private CheckInfo m_readInfo;
        private CheckInfo m_writeInfo;
        private CheckInfo m_errorInfo;
        private Dictionary<SocketHandler, CheckFlag> m_socketStates;
        private int m_timeout;

        public delegate void Dispatch(Dictionary<SocketHandler, CheckFlag> source);
        private Dispatch m_dispatch;

        /// <summary>
        /// Listening Socket state.
        /// </summary>
        /// <param name="capacity">init capacity</param>
        /// <param name="timeout">Select timeout (microsecond).</param>
        public SocketListener(int capacity, int timeout)
        {
            m_readInfo = new CheckInfo(capacity, CheckFlag.Read);
            m_writeInfo = new CheckInfo(capacity, CheckFlag.Write);
            m_errorInfo = new CheckInfo(capacity, CheckFlag.Error);
            m_socketStates = new Dictionary<SocketHandler, CheckFlag>(capacity);
            m_timeout = timeout;
        }

        /// <summary>
        /// 注册监听
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="flag"></param>
        public void Register(SocketHandler handler, CheckFlag flag)
        {
            if ((flag & CheckFlag.Read) != 0)
            {
                Register(handler, m_readInfo);
            }
            if ((flag & CheckFlag.Write) != 0)
            {
                Register(handler, m_writeInfo);
            }
            if ((flag & CheckFlag.Error) != 0)
            {
                Register(handler, m_errorInfo);
            }
        }

        private void Register(SocketHandler handler, CheckInfo checkInfo)
        {
            //lock (checkInfo.sockets)
            LockRun(ref checkInfo.spinLock, () =>
            {
                Socket socket = handler.GetSocket().Socket;
                if (!checkInfo.waitSockets.ContainsKey(socket))
                {
                    checkInfo.waitSockets.Add(socket, handler);
                }
            });
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="flag"></param>
        public void UnRegister(SocketHandler handler, CheckFlag flag)
        {
            if ((flag & CheckFlag.Read) != 0)
            {
                UnRegister(handler, m_readInfo);
            }
            if ((flag & CheckFlag.Write) != 0)
            {
                UnRegister(handler, m_writeInfo);
            }
            if ((flag & CheckFlag.Error) != 0)
            {
                UnRegister(handler, m_errorInfo);
            }
        }

        private void UnRegister(SocketHandler handler, CheckInfo checkInfo)
        {
            //lock (checkInfo.sockets)
            LockRun(ref checkInfo.spinLock, () =>
            {
                checkInfo.waitSockets.Remove(handler.GetSocket().Socket);
            });
        }

        public void SetDispatch(Dispatch dispatch)
        {
            m_dispatch = dispatch;
        }

        public void Listen()
        {
            BuildCheckList(m_readInfo);
            BuildCheckList(m_writeInfo);
            BuildCheckList(m_errorInfo);

            if (m_readInfo.checkSockets.Count > 0 || m_writeInfo.checkSockets.Count > 0 || m_errorInfo.checkSockets.Count > 0)
            {
                try
                {
                    Select();

                    m_socketStates.Clear();
                    ExecuteCheckList(m_readInfo);
                    ExecuteCheckList(m_writeInfo);
                    ExecuteCheckList(m_errorInfo);
                    m_dispatch(m_socketStates);
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                }
            }
            else
            {
                Sleep();
            }
        }

        /// <summary>
        /// 构建Select所需的checkList
        /// </summary>
        /// <param name="checkList"></param>
        /// <param name="source"></param>
        void BuildCheckList(CheckInfo info)
        {
            info.checkSockets.Clear();
            //lock (source)
            LockRun(ref info.spinLock, () =>
            {
                var e = info.waitSockets.GetEnumerator();
                while (e.MoveNext())
                {
                    if (!e.Current.Value.Busy)
                    {
                        info.checkSockets.Add(e.Current.Key);
                    }
                }
            });
        }

        void ExecuteCheckList(CheckInfo info)
        {
            if (info.checkSockets.Count > 0)
            {
                //lock (source)
                LockRun(ref info.spinLock, () =>
                {
                    for (int i = 0; i < info.checkSockets.Count; ++i)
                    {
                        var s = info.checkSockets[i];
                        if (info.waitSockets.ContainsKey(s))
                        {
                            var h = info.waitSockets[s];

                            if (!h.SetBusy(true))
                                continue;

                            if (m_socketStates.ContainsKey(h))
                                m_socketStates[h] |= info.checkFlag;
                            else
                                m_socketStates.Add(h, info.checkFlag);
                        }
                    }
                }); // end lock
            }
        } // end ExecuteCheckList

        void LockRun(ref SpinLock spinLock, Action action)
        {
            bool gotLock = false;
            try
            {
                spinLock.Enter(ref gotLock);
                action();
            }
            finally
            {
                if (gotLock) spinLock.Exit();
            }
        }

        void Select()
        {
            Socket.Select(m_readInfo.checkSockets, m_writeInfo.checkSockets, m_errorInfo.checkSockets, m_timeout);
        }

        void Sleep()
        {
            System.Threading.Thread.Sleep(m_timeout / 1000);
        }
    }
}
