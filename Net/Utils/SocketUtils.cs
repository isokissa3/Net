﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Utils
{
    internal static class SocketUtils
    {
        internal static int GetAddressFamilyLength(AddressFamily addressFamily)
        {
            return addressFamily switch
            {
                AddressFamily.InterNetwork => 4,
                AddressFamily.InterNetworkV6 => 16,
                _ => throw new NotSupportedException()
            };
        }
    }
}
