﻿using System;
using System.Net;
using System.Net.Sockets;

namespace CsNet
{
    public class SocketBase : IDisposable
    {
        public delegate void FCallback(FResult ret, int code, string msg);

        protected delegate FResult FAction(ref int errorCode, ref string errorMsg, out SocketError socketError);

        protected Socket m_socket;
        protected FResult m_state;
        protected int m_errorCode;
        protected string m_errorMsg;

        protected int m_realSend;
        protected int m_realRecv;

        protected EndPoint m_remoteEndPoint;

        public SocketBase(AddressFamily af, SocketType st, ProtocolType pt)
        {
            Init(new Socket(af, st, pt));
        }

        public SocketBase(Socket socket)
        {
            Init(socket);
        }

        private SocketBase()
        {
        }

        ~SocketBase()
        {
            Dispose();
        }

        private void Init(Socket socket)
        {
            m_socket = socket;
            m_state = FResult.Success;
            m_errorCode = 0;
            m_errorMsg = null;
            m_realSend = 0;
            m_realRecv = 0;
        }

        public virtual FResult Bind(EndPoint ep)
        {
            return DoAction(() =>
            {
                m_socket.Bind(ep);
            });
        }

        public virtual FResult Listen(int backlog)
        {
            return DoAction(() =>
            {
                m_socket.Listen(backlog);
            });
        }

        public virtual Socket Accept()
        {
            return DoAction(() =>
            {
                return m_socket.Accept();
            });
        }

        public virtual FResult Connect(EndPoint ep)
        {
            m_remoteEndPoint = ep;
            return DoAction(() =>
            {
                m_socket.Connect(ep);
            });
        }

        public virtual FResult Disconnect(bool reuseSocket)
        {
            return DoAction(() =>
            {
                m_socket.Disconnect(reuseSocket);
            });
        }

        public virtual FResult Reconnect()
        {
            if (m_remoteEndPoint == null)
            {
                m_errorCode = (int)FResult.Error;
                m_errorMsg = "Must call Connect before Reconnect.";
                return FResult.Error;
            }
            Shutdown(SocketShutdown.Both);
            Close();
            Socket socket = new Socket(m_socket.AddressFamily, m_socket.SocketType, m_socket.ProtocolType); // !!!
            m_socket.Dispose();
            m_socket = socket;
            return Connect(m_remoteEndPoint);
        }

        public bool Connected(bool current)
        {
            if (!current)
                return m_socket.Connected;

            if (!m_socket.Connected)
                return false;

            bool blocking = m_socket.Blocking;
            m_socket.Blocking = false;
            var ret = Send(new byte[0], 0, 0);
            m_socket.Blocking = blocking;

            return ret == FResult.Success || ret == FResult.WouldBlock;
        }

        public virtual FResult Send(byte[] buffer, int offset, int size)
        {
            return DoAction((ref int errorCode, ref string errorMsg, out SocketError socketError) =>
            {
                FResult ret = FResult.Success;
                socketError = SocketError.Success;
                try
                {
                    int nsend = m_socket.Send(buffer, offset, size, SocketFlags.None, out socketError);
                    if (socketError == SocketError.Success)
                    {
                        if (nsend < size)
                        {
                            m_realSend = nsend;
                            ret = FResult.WouldBlock;
                            errorCode = 0;
                            errorMsg = "";
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == (int)SocketError.WouldBlock)
                    {
                        m_realSend = 0;
                        ret = FResult.WouldBlock;
                        errorCode = 0;
                        errorMsg = "";
                    }
                    else
                    {
                        throw e;
                    }
                }
                return ret;
            });
        }

        public virtual FResult Recv(byte[] buffer, int offset, int size)
        {
            return DoAction((ref int errorCode, ref string errorMsg, out SocketError socketError) =>
            {
                FResult ret = FResult.Success;
                socketError = SocketError.Success;
                try
                {
                    int nrecv = m_socket.Receive(buffer, offset, size, SocketFlags.None, out socketError);
                    if (socketError == SocketError.Success)
                    {
                        if (nrecv == 0)
                        {
                            ret = FResult.SocketClosed;
                            errorCode = (int)FResult.SocketClosed;
                            errorMsg = "";
                        }
                        else if (nrecv < size)
                        {
                            m_realRecv = nrecv;
                            ret = FResult.WouldBlock;
                            errorCode = 0;
                            errorMsg = "";
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == (int)SocketError.WouldBlock)
                    {
                        m_realRecv = 0;
                        ret = FResult.WouldBlock;
                        errorCode = 0;
                        errorMsg = "";
                    }
                    else
                    {
                        throw e;
                    }
                }
                return ret;
            });
        }

        public virtual FResult BeginConnect(EndPoint ep, FCallback callback)
        {
            AsyncCallback cb = new AsyncCallback((IAsyncResult ar) =>
            {
                DoAction(() =>
                {
                    Socket socket = (Socket)ar.AsyncState;
                    socket.EndConnect(ar);
                }, callback);
            });

            return DoAction(() =>
            {
                m_socket.BeginConnect(ep, cb, m_socket);
            });
        }

        public virtual FResult BeginSend(byte[] buffer, int offset, int size, FCallback callback)
        {
            AsyncCallback cb = new AsyncCallback((IAsyncResult ar) =>
            {
                DoAction((ref int errorCode, ref string errorMsg, out SocketError socketError) =>
                {
                    FResult ret = FResult.Success;
                    Socket socket = (Socket)ar.AsyncState;
                    int nsend = socket.EndSend(ar, out socketError);
                    if (socketError == SocketError.Success)
                    {
                        if (nsend < size)
                        {
                            m_realSend = nsend;
                            ret = FResult.WouldBlock;
                            errorCode = 0;
                            errorMsg = "";
                        }
                    }
                    return ret;
                }, callback);
            });

            return DoAction((ref int errorCode, ref string errorMsg, out SocketError socketError) =>
            {
                m_socket.BeginSend(buffer, offset, size, SocketFlags.None, out socketError, cb, m_socket);
                return FResult.Success;
            });
        }

        public virtual FResult BeginRecv(byte[] buffer, int offset, int size, FCallback callback)
        {
            AsyncCallback cb = new AsyncCallback((IAsyncResult ar) =>
            {
                DoAction((ref int errorCode, ref string errorMsg, out SocketError socketError) =>
                {
                    FResult ret = FResult.Success;
                    Socket socket = (Socket)ar.AsyncState;
                    int nrecv = socket.EndReceive(ar, out socketError);
                    if (socketError == SocketError.Success)
                    {
                        if (nrecv < size)
                        {
                            m_realRecv = nrecv;
                            ret = FResult.WouldBlock;
                            errorCode = 0;
                            errorMsg = "";
                        }
                    }
                    return ret;
                }, callback);
            });

            return DoAction((ref int errorCode, ref string errorMsg, out SocketError socketError) =>
            {
                m_socket.BeginSend(buffer, offset, size, SocketFlags.None, out socketError, cb, m_socket);
                return FResult.Success;
            });
        }

        public virtual FResult Shutdown(SocketShutdown how)
        {
            return DoAction(() =>
            {
                m_socket.Shutdown(how);
            });
        }

        public virtual FResult Close()
        {
            Shutdown(SocketShutdown.Both);
            return DoAction(() =>
            {
                m_socket.Close();
            });
        }

        public void Dispose()
        {
            if (m_socket != null)
            {
                Close();
                m_socket.Dispose();
                m_socket = null;
            }
            m_errorMsg = null;
            m_remoteEndPoint = null;
            GC.SuppressFinalize(this);
        }

        protected FResult DoAction(System.Action action)
        {
            m_state = FResult.Success;
            try
            {
                action();
            }
            catch (SocketException e)
            {
                m_state = FResult.SocketException;
                m_errorCode = e.ErrorCode;
                m_errorMsg = e.Message;
            }
            catch (Exception e)
            {
                m_state = FResult.Exception;
                m_errorCode = (int)FResult.Exception;
                m_errorMsg = e.Message;
            }
            return m_state;
        }

        protected T DoAction<T>(System.Func<T> action)
        {
            T result;
            m_state = FResult.Success;
            try
            {
                result = action();
            }
            catch (SocketException e)
            {
                m_state = FResult.SocketException;
                m_errorCode = e.ErrorCode;
                m_errorMsg = e.Message;
                result = default(T);
            }
            catch (Exception e)
            {
                m_state = FResult.Exception;
                m_errorCode = (int)FResult.Exception;
                m_errorMsg = e.Message;
                result = default(T);
            }
            return result;
        }

        protected FResult DoAction(FAction action)
        {
            m_state = FResult.Success;
            try
            {
                SocketError socketError;
                m_state = action(ref m_errorCode, ref m_errorMsg, out socketError);
                if (socketError != SocketError.Success)
                {
                    m_state = FResult.SocketError;
                    m_errorCode = (int)socketError;
                    m_errorMsg = "";
                }
            }
            catch (SocketException e)
            {
                m_state = FResult.SocketException;
                m_errorCode = e.ErrorCode;
                m_errorMsg = e.Message;
            }
            catch (Exception e)
            {
                m_state = FResult.Exception;
                m_errorCode = (int)FResult.Exception;
                m_errorMsg = e.Message;
            }
            return m_state;
        }

        protected void DoAction(System.Action action, FCallback callback)
        {
            FResult ret = FResult.Success;
            int errorCode = 0;
            string errorMsg = null;

            try
            {
                action();
            }
            catch (SocketException e)
            {
                ret = FResult.SocketException;
                errorCode = e.ErrorCode;
                errorMsg = e.Message;
            }
            catch (Exception e)
            {
                ret = FResult.Exception;
                errorCode = (int)FResult.Exception;
                errorMsg = e.Message;
            }
            finally
            {
                callback(ret, errorCode, errorMsg);
            }
        }

        protected void DoAction(FAction action, FCallback callback)
        {
            FResult ret = FResult.Success;
            int errorCode = 0;
            string errorMsg = null;
            SocketError socketError = SocketError.Success;
            try
            {
                ret = action(ref errorCode, ref errorMsg, out socketError);
                if (socketError != SocketError.Success)
                {
                    ret = FResult.SocketError;
                    errorCode = (int)socketError;
                }
            }
            catch (SocketException e)
            {
                ret = FResult.SocketException;
                errorCode = e.ErrorCode;
                errorMsg = e.Message;
            }
            catch (Exception e)
            {
                ret = FResult.Exception;
                errorCode = (int)FResult.Exception;
                errorMsg = e.Message;
            }
            finally
            {
                callback(ret, errorCode, errorMsg);
            }
        }

        public Socket Socket { get { return m_socket; } }

        public FResult State { get { return m_state; } }
        public int ErrorCode { get { return m_errorCode; } }
        public string ErrorMsg { get { return m_errorMsg; } }

        public int RealSend { get { return m_realSend; } }
        public int RealRecv { get { return m_realRecv; } }
    }
}
