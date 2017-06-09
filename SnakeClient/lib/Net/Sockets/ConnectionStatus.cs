using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.Net.Sockets
{
    public enum ConnectionStatus
    {
        NotConnected, Connecting, Connected
    }

    public enum SocketServerStatus
    {
        None, Binded, Listened, Accepting
    }
}
