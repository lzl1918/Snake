using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace lib.Net.Sockets
{
    public class SocketServer : IDisposable
    {
        List<SocketClient> clients = new List<SocketClient>();
        Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        SocketServerStatus status = SocketServerStatus.None;

        bool isInAcceptProgress = false;
        object locker = new object();
        ManualResetEvent sendDone = new ManualResetEvent(false);

        public List<SocketClient> Clients
        {
            get
            {
                return clients;
            }
            private set
            {
                clients = value;
            }
        }
        public SocketServerStatus Status
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

        public event SocketServerConnectionReceivedHandler ConnectionReceived;
        public event SocketMsgReceivedHandler MessageReceived;
        public event SocketDisconnectedHandler ClientDisconnected;

        public void ListenBind(int port)
        {
            IPAddress ipaddr = IPAddress.Any;
            try
            {
                server.NoDelay = true;
                server.Bind(new IPEndPoint(ipaddr, port));
                status = SocketServerStatus.Binded;
                server.Listen(10);
                status = SocketServerStatus.Listened;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void BeginAccept()
        {
            if (isInAcceptProgress == true)
                return;
            isInAcceptProgress = true;
            server.BeginAccept(AcceptCallback, server);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket server = (Socket)ar.AsyncState;
            SocketClient client = new SocketClient(server.EndAccept(ar));
            if (clients.Contains(client))
            {
                return;
            }
            else
            {
                client.MessageReceived += Client_MessageReceived;
                client.Disconnected += Client_Disconnected;
                client.BeginReceive();
                clients.Add(client);
                if (ConnectionReceived != null)
                    ConnectionReceived(this, client);

            }

            server.BeginAccept(AcceptCallback, server);
        }

        private void Client_Disconnected(object sender, SocketDisconnectedEventArgs args)
        {
            SocketClient client = sender as SocketClient;
            args.Index = Clients.IndexOf(client);
            if (args.Index == -1)
            {
                throw new Exception("Unknow");
            }
            Clients.RemoveAt(args.Index);
            if (ClientDisconnected != null)
                ClientDisconnected(this, args);
        }

        private void Client_MessageReceived(SocketClient socket, SocketMessageEventArgs args)
        {
            if (MessageReceived != null)
                MessageReceived(socket, args);
        }

        public Task SendToAllAsync(byte[] data)
        {
            return Task.Factory.StartNew(() =>
            {
                lock(locker)
                {
                    if (clients.Count == 0)
                        return;

                    sendDone.Reset();
                    SocketMessage msg = SocketMessage.GetDataMessage(data);
                    byte[] buf = msg.GetBuffer();
                    Task[] tasks = (from client in clients
                                    select client.SendRawDataAsync(buf)).ToArray();
                    Task.Factory.ContinueWhenAll(tasks, (ts) =>
                    {
                        sendDone.Set();
                        return;
                    });
                    sendDone.WaitOne();
                }
            });

        }
        public Task SendToAllAsync(string str)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (locker)
                {
                    sendDone.Reset();
                    SocketMessage msg = SocketMessage.GetStringMessage(str);
                    byte[] buf = msg.GetBuffer();
                    Task[] tasks = (from client in clients
                                    select client.SendRawDataAsync(buf)).ToArray();
                    Task.Factory.ContinueWhenAll(tasks, (ts) =>
                    {
                        sendDone.Set();
                        return;
                    });
                    sendDone.WaitOne();
                }
            });

        }
        public Task SendToAllAsync(ITransferableObject obj, int objMark)
        {
            return Task.Factory.StartNew(() =>
            {
                lock (locker)
                {
                    sendDone.Reset();
                    SocketMessage msg = SocketMessage.GetObjectMessage(obj);
                    byte[] buf = msg.GetBuffer();
                    Task[] tasks = (from client in clients
                                    select client.SendRawDataAsync(buf)).ToArray();
                    Task.Factory.ContinueWhenAll(tasks, (ts) =>
                    {
                        sendDone.Set();
                        return;
                    });
                    sendDone.WaitOne();
                }
            });

        }

        public void Dispose()
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
            server.Disconnect(false);
            server.Dispose();
        }
    }
}
