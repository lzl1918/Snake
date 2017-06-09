using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace lib.Net.Sockets
{
    public delegate void SocketServerConnectionReceivedHandler(SocketServer sender, SocketClient socket);
    public delegate void SocketMsgReceivedHandler(SocketClient socket, SocketMessageEventArgs msg);
    public delegate void SocketDisconnectedHandler(object sender, SocketDisconnectedEventArgs args);

    public class SocketDisconnectedEventArgs
    {
        private bool isCauseByException = true;
        SocketClient peer;
        int index = 0;

        public bool IsCauseByException
        {
            get
            {
                return isCauseByException;
            }

            internal set
            {
                isCauseByException = value;
            }
        }

        public SocketClient Peer
        {
            get
            {
                return peer;
            }
            internal set
            {
                peer = value;
            }
        }

        public int Index
        {
            get
            {
                return index;
            }
            internal set
            {
                index = value;
            }
        }

        internal SocketDisconnectedEventArgs(SocketClient peer, bool isexception, int index = 0)
        {
            this.peer = peer;
            this.isCauseByException = isexception;
            this.index = index;
        }
    }
}
