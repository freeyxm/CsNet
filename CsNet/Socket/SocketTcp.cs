﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace CsNet
{
    public class SocketTcp : SocketBase
    {
        public SocketTcp(AddressFamily af)
            : base(af, SocketType.Stream, ProtocolType.Tcp)
        {
        }
    }
}
