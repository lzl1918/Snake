using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace lib.Net.Sockets
{
    public class SocketClient : IDisposable
    {
        class ReceiveInformation
        {
            public byte[] buffer = null;
            public Socket socket = null;
            public int lastoffset = 0;
            public int lastindex = 0;
        }
        ReceiveInformation recvinf = new ReceiveInformation();

        ManualResetEvent connectDone = new ManualResetEvent(false);
        ManualResetEvent sendDone = new ManualResetEvent(false);

        CancellationTokenSource recvTokenSource = null;
        object locker = new object();

        Socket socket = null;
        ConnectionStatus status = ConnectionStatus.NotConnected;

        public ConnectionStatus Status
        {
            get
            {
                return status;
            }
            private set
            {
                status = value;
            }
        }
        public EndPoint RemoteAddress
        {
            get
            {
                if (Status != ConnectionStatus.Connected)
                    throw new InvalidOperationException("套接字未连接");
                return Socket.RemoteEndPoint;
            }
        }

        public Socket Socket
        {
            get
            {
                return socket;
            }
            private set
            {
                socket = value;
            }
        }

        public event SocketMsgReceivedHandler MessageReceived;
        public event SocketDisconnectedHandler Disconnected;

        public SocketClient()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            Status = ConnectionStatus.NotConnected;
        }
        internal SocketClient(Socket soc)
        {
            Socket = soc;
            soc.NoDelay = true;
            Status = soc.Connected ? ConnectionStatus.Connected : ConnectionStatus.NotConnected;
        }


        public Task ConnectAsync(string host, int port)
        {
            if (Status != ConnectionStatus.NotConnected)
                throw new InvalidOperationException("套接字不处于未连接状态, 无法尝试连接操作");

            DnsEndPoint endp = null;
            try
            {
                endp = new DnsEndPoint(host, port);
            }
            catch
            {
                throw new ArgumentException("无法解析主机地址");
            }
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    connectDone.Reset();
                    Status = ConnectionStatus.Connecting;
                    socket.BeginConnect(endp, ConnectCallback, socket);
                    connectDone.WaitOne();
                    Status = ConnectionStatus.Connected;
                    BeginReceive();
                }
                catch (Exception ex)
                {
                    Status = ConnectionStatus.NotConnected;
                    throw ex;

                }
            });
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            connectDone.Set();
        }

        internal void BeginReceive()
        {
            lock (locker)
            {
                if (recvTokenSource != null)
                    throw new Exception("接收已经开始");

                recvinf.buffer = new byte[4096];
                recvinf.socket = socket;
                recvinf.lastoffset = 0;
                recvTokenSource = new CancellationTokenSource();
                socket.BeginReceive(recvinf.buffer, 0, recvinf.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), recvinf);

            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            lock (locker)
            {
                ReceiveInformation recvinf = (ReceiveInformation)ar.AsyncState;
                int len = 0;
                bool needMore = false;
                int lastoffset = 0;
                try
                {
                    len = socket.EndReceive(ar);

                    if (len <= 0)
                    {
                        DisconnectedReceived(false);
                    }
                    else
                    {
                        var messages = new SocketMessageEventArgs(recvinf.buffer, recvinf.lastoffset + len);
                        messages.Messages = SocketMessage.Resolve(recvinf.buffer, recvinf.lastindex, recvinf.lastoffset + len - 1, out needMore, out lastoffset);

                        if (messages.Messages.Count > 0)
                            if (MessageReceived != null)
                                MessageReceived(this, messages);

                        if (needMore == false)
                        {
                            recvinf.lastoffset = 0;
                            recvinf.lastindex = 0;
                            recvinf.socket.BeginReceive(recvinf.buffer, 0, recvinf.buffer.Length, SocketFlags.None, ReceiveCallback, recvinf);
                        }
                        else
                        {
                            recvinf.lastoffset = recvinf.lastindex + len;
                            recvinf.lastindex = lastoffset;
                            recvinf.socket.BeginReceive(recvinf.buffer, recvinf.lastoffset, recvinf.buffer.Length - recvinf.lastoffset, SocketFlags.None, ReceiveCallback, recvinf);
                        }


                    }
                }
                catch (SocketException sex)
                {
                    switch ((SocketExceptionErrorCode)sex.ErrorCode)
                    {
                        case SocketExceptionErrorCode.ConnectionForciblyClosed:
                            DisconnectedReceived(true);
                            break;
                    }
                }
            }
        }

        private void DisconnectedReceived(bool isException)
        {
            status = ConnectionStatus.NotConnected;
            if (Disconnected != null)
                Disconnected(this, new SocketDisconnectedEventArgs(this, isException));
        }

        internal Task SendRawDataAsync(byte[] data)
        {
            return Task.Factory.StartNew(() =>
            {
                if (Status != ConnectionStatus.Connected)
                    throw new InvalidOperationException("套接字未连接, 无法发送数据");
                try
                {
                    sendDone.Reset();
                    socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, socket);
                    sendDone.WaitOne();
                }
                catch (Exception ex)
                {
                    throw new Exception("发送数据失败");
                }
            });
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            int actuallen = socket.EndSend(ar);
            sendDone.Set();
        }

        public Task SendAsync(byte[] data)
        {
            SocketMessage msg = SocketMessage.GetDataMessage(data);
            return SendRawDataAsync(msg.GetBuffer());
        }

        public Task SendAsync(string str)
        {
            SocketMessage msg = SocketMessage.GetStringMessage(str);
            return SendRawDataAsync(msg.GetBuffer());
        }

        public Task SendAsync(ITransferableObject obj, int objMark)
        {
            SocketMessage msg = SocketMessage.GetObjectMessage(obj);
            return SendRawDataAsync(msg.GetBuffer());
        }

        public void Dispose()
        {
            recvTokenSource.Cancel();
            if (Status == ConnectionStatus.Connected)
                Socket.Disconnect(false);
            socket.Shutdown(SocketShutdown.Both);
            Socket.Dispose();

        }
    }
}
