using lib.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using lib;
using System.Windows.Media.Animation;

namespace SnakeServerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Map map = null;
        SocketServer server = null;
        ObservableCollection<SocketClient> clients = new ObservableCollection<SocketClient>();
        PlayerStatus[] playerStatus = null;
        bool isGameStarted = false;
        System.Timers.Timer timer = null;
        Dictionary<MapType, string> fruitString = new Dictionary<MapType, string>()
        {
            { MapType.BlindFruit, "致盲"}, { MapType.HardFruit, "硬邦邦"}, { MapType.RushFruit, "跑快快"}, {MapType.SlowFruit, "别动" }
        };

        public MainWindow()
        {

            InitializeComponent();
            btn_startListen.Click += Btn_startListen_Click;
        }

        private void Btn_startListen_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation transformDa = new DoubleAnimation()
            {
                To = -90,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            (sp_topBarContainer.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, transformDa);

            grid_Connection.RowDefinitions.Clear();
            grid_Connection.Children.Clear();
            for (int i = 0; i < (int)slider_players.Value; i++)
            {
                grid_Connection.RowDefinitions.Add(new RowDefinition());
                TextBlock tbl = new TextBlock()
                {
                    Text = string.Format("玩家{0}", i + 1),
                    VerticalAlignment = VerticalAlignment.Center
                };
                TextBlock tbr = new TextBlock()
                {
                    Text = "未连接",
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5)
                };
                Button btn = new Button()
                {
                    Content = "强制变短",
                    Tag = i,
                    IsEnabled = false
                };
                btn.Click += ForceShortenBtn_Click;
                grid_Connection.Children.Add(tbl);
                grid_Connection.Children.Add(tbr);
                grid_Connection.Children.Add(btn);
                Grid.SetRow(tbl, i);
                Grid.SetRow(tbr, i);
                Grid.SetRow(btn, i);
                Grid.SetColumn(tbl, 0);
                Grid.SetColumn(tbr, 1);
                Grid.SetColumn(btn, 2);
            }

            map = new Map((int)slider_players.Value, int.Parse(tb_stageWidth.Text), int.Parse(tb_stageHeight.Text));
            map.HardChanged += Map_HardChanged;
            playerStatus = new PlayerStatus[map.SnakeCount];
            server = new SocketServer();
            server.ConnectionReceived += Server_ConnectionReceived;
            server.MessageReceived += Server_MessageReceived;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.ListenBind(int.Parse(tb_port.Text));
            server.BeginAccept();
        }

        private void Map_HardChanged(object sender, HardChangedArgs e)
        {
            server.SendToAllAsync(new byte[] { (byte)DataType.HardChanged, (byte)e.Index, (byte)(e.IsSetHard == true ? 1 : 0) });
            Dispatcher.BeginInvoke(new Action(() =>
            {

            }));
        }

        private void Server_ClientDisconnected(object sender, SocketDisconnectedEventArgs args)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                int index = clients.IndexOf(args.Peer);
                if (index < 0)
                {
                    int i = 0;
                }
                this.Title = "Disconnected, " + index;
                map.ForceShorten(index, 0);
                (grid_Connection.Children[index * 3 + 1] as TextBlock).Text = "断开";
                clients[index] = null;
                playerStatus[index] = PlayerStatus.Disconnected;
                server.SendToAllAsync(new byte[] { (byte)DataType.PlayerDisconnected, (byte)index });

                int liveCount = 0, liveIndex = -1;
                for (int i = 0; i < map.SnakeCount; i++)
                {
                    if (playerStatus[i] == PlayerStatus.Playing)
                    {
                        liveCount++;
                        liveIndex = i;
                    }
                }
                if (liveCount == 1 && map.SnakeCount > 1)
                {
                    server.SendToAllAsync(new byte[] { (byte)lib.DataType.GameEnd, (byte)liveIndex });
                    timer.Enabled = false;
                }

            }));
        }

        private void ForceShortenBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int index = (int)btn.Tag;
            lock (map)
            {
                map.ForceShorten(index, 1);
                var changes = map.GetChanges();
                if (changes.Count > 0)
                {
                    byte[] buff = new byte[1] { (byte)lib.DataType.MapData }.Concat(GetBuffer(changes)).ToArray();
                    server.SendToAllAsync(buff);
                }
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateMap(changes);
                }));
            }
        }

        private void UpdateMap(Dictionary<Coord, MapType> changes)
        {
            foreach (var item in changes)
            {
                TextBlock txtb = (grid_map.Children[item.Key.Y * map.Width + item.Key.X] as Grid).Children[0] as TextBlock;
                if (item.Value == MapType.Empty)
                    txtb.Text = "";
                else if (item.Value == MapType.Fruit)
                    txtb.Text = "+";
                else if (item.Value == MapType.ShortenFruit)
                    txtb.Text = "-";
                else if (item.Value == MapType.SuperFruit)
                    txtb.Text = "+5";
                else if (item.Value == MapType.SuperShortenFruit)
                    txtb.Text = "-5";
                else if (item.Value == MapType.HiddenSuperFruitAsShortenFruit)
                    txtb.Text = "!+5";
                else if (item.Value == MapType.HiddenSuperShortenFruitAsSuperFruit)
                    txtb.Text = "!-5";
                else if (item.Value == MapType.BlindFruit)
                    txtb.Text = "b";
                else if (item.Value == MapType.SlowFruit)
                    txtb.Text = "s";
                else if (item.Value == MapType.RushFruit)
                    txtb.Text = "r";
                else if (item.Value == MapType.HardFruit)
                    txtb.Text = "h";
                else if (item.Value >= MapType.SnakeHeadStart && item.Value <= MapType.SnakeHeadStart + (byte)map.SnakeCount - 1)
                {
                    if (map.IsHard[item.Value - MapType.SnakeHeadStart] == true)
                        txtb.Text = (item.Value - MapType.SnakeHeadStart).ToString() + "H";
                    else
                        txtb.Text = (item.Value - MapType.SnakeHeadStart).ToString();
                }
                else if (item.Value >= MapType.SnakeStart && item.Value <= MapType.SnakeStart + (byte)map.SnakeCount - 1)
                    txtb.Text = (item.Value - MapType.SnakeStart).ToString();
            }
        }

        private void Server_ConnectionReceived(SocketServer sender, SocketClient socket)
        {
            lock (clients)
            {
                clients.Add(socket);
                int i = clients.Count;
                playerStatus[i - 1] = PlayerStatus.Connected;
                socket.SendAsync(new byte[] { (byte)DataType.PlayerIndex, (byte)(i - 1) });
                this.Dispatcher.BeginInvoke(new Action<int>((num) =>
                {
                    (grid_Connection.Children[num * 3] as TextBlock).Text = socket.RemoteAddress.ToString();
                    (grid_Connection.Children[num * 3 + 1] as TextBlock).Text = "连接";
                }), System.Windows.Threading.DispatcherPriority.Normal, i - 1);

                byte[] buf = new byte[1] { (byte)lib.DataType.StageSizeQuery };
                buf = buf.Concat(BitConverter.GetBytes(map.Height)).Concat(BitConverter.GetBytes(map.Width)).ToArray();
                socket.SendAsync(buf);
                buf = new byte[2] { (byte)lib.DataType.PlayerStatus, (byte)map.SnakeCount }.Concat(from elem in playerStatus select (byte)elem).ToArray();
                server.SendToAllAsync(buf).Wait();

                for(i = 0; i < map.SnakeCount; i++)
                {
                    byte[] buff = new byte[] { (byte)lib.DataType.SetName, (byte)i }.Concat(Encoding.UTF8.GetBytes(map.SnakeNames[i])).ToArray();
                    socket.SendAsync(buff).Wait();
                }
            }
        }

        private void Server_MessageReceived(SocketClient socket, SocketMessageEventArgs args)
        {
            int socketindex = clients.IndexOf(socket);
            foreach (var msg in args.Messages)
            {
                if (msg.MsgType == MessageType.Data)
                {
                    switch ((lib.DataType)msg.DataMessage.Data[0])
                    {
                        case DataType.SetName:
                            byte tindex = msg.DataMessage.Data[1];
                            string name = Encoding.UTF8.GetString(msg.DataMessage.Data, 2, (int)msg.DataMessage.Length - 2);
                            map.SnakeNames[tindex] = name;
                            server.SendToAllAsync(msg.DataMessage.Data);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {

                            }));
                            break;

                        case DataType.UseInventory:
                            byte toplayer = msg.DataMessage.Data[2];
                            MapType inventoryType = (MapType)msg.DataMessage.Data[1];

                            if (map.Snakes[socketindex].Inventory.ContainsKey(inventoryType))
                            {
                                map.Snakes[socketindex].Inventory[inventoryType]--;
                                socket.SendAsync(new byte[] { (byte)DataType.AddInventory, (byte)inventoryType, map.Snakes[socketindex].Inventory[inventoryType] }).Wait();
                                if (map.Snakes[socketindex].Inventory[inventoryType] == 0)
                                    map.Snakes[socketindex].Inventory.Remove(inventoryType);

                                clients[toplayer].SendAsync(new byte[] { (byte)DataType.UseInventory, (byte)inventoryType, 1 }).Wait();

                                if (socketindex == toplayer)
                                    server.SendToAllAsync(new ServerNotification()
                                    {
                                        Index = toplayer,
                                        PlayerMessage = string.Format("你对自己使用了 {0}", fruitString[inventoryType]),
                                        ElseMessage = string.Format("{0} 对 他自己 使用了 {1}", map.SnakeNames[toplayer], fruitString[inventoryType])
                                    }, 1);
                                else
                                    server.SendToAllAsync(new ServerNotification()
                                    {
                                        Index = toplayer,
                                        PlayerMessage = string.Format("{0} 对 你 使用了 {1}", map.SnakeNames[socketindex], fruitString[inventoryType]),
                                        ElseMessage = string.Format("{0} 对 {1} 使用了 {2}", map.SnakeNames[socketindex], map.SnakeNames[toplayer], fruitString[inventoryType])
                                    }, 1);

                                switch (inventoryType)
                                {
                                    case MapType.BlindFruit:
                                        break;
                                    case MapType.RushFruit:
                                        map.ChangeSnakeSpeed(toplayer, 1);
                                        break;
                                    case MapType.SlowFruit:
                                        map.ChangeSnakeSpeed(toplayer, -1);
                                        break;
                                    case MapType.HardFruit:
                                        map.SetSnakeHard(toplayer);
                                        break;
                                }

                            }
                            break;

                        case lib.DataType.PingTest:
                            socket.SendAsync(msg.DataMessage.Data).Wait();
                            break;

                        case lib.DataType.SnakeData:
                            break;

                        case lib.DataType.ChangeDirection:
                            short x = (short)(msg.DataMessage.Data[1] - 1);
                            short y = (short)(msg.DataMessage.Data[2] - 1);
                            map.Snakes[socketindex].Direction = new Coord(x, y);
                            break;

                        case lib.DataType.Prepared:
                            playerStatus[socketindex] = PlayerStatus.Prepared;
                            this.Dispatcher.BeginInvoke(new Action<int>((num) =>
                            {
                                (grid_Connection.Children[num * 3 + 1] as TextBlock).Text = "准备";
                            }), System.Windows.Threading.DispatcherPriority.Normal, socketindex).Wait();

                            byte[] buf = new byte[2] { (byte)lib.DataType.PlayerStatus, (byte)map.SnakeCount }
                                                     .Concat(from elem in playerStatus
                                                             select (byte)elem)
                                                     .ToArray();
                            server.SendToAllAsync(buf).Wait();

                            bool prepared = isGameStarted == false;
                            for (int i = 0; i < map.SnakeCount; i++)
                                prepared = prepared && playerStatus[i] == PlayerStatus.Prepared;
                            if (prepared == true)
                                this.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    GameStart();
                                })).Wait();
                            break;
                    }
                }
            }
        }

        private void GameStart()
        {
            if (isGameStarted == true)
                return;

            isGameStarted = true;
            server.SendToAllAsync(new byte[1] { (byte)lib.DataType.GameStart }).Wait();

            int i = 0, j = 0;
            Grid grid;
            TextBlock tb;
            grid_map.Children.Clear();
            grid_map.RowDefinitions.Clear();
            grid_map.ColumnDefinitions.Clear();
            for (i = 0; i < map.Width; i++)
                grid_map.ColumnDefinitions.Add(new ColumnDefinition());
            for (j = 0; j < map.Height; j++)
                grid_map.RowDefinitions.Add(new RowDefinition());
            for (i = 0; i < map.Height; i++)
            {
                for (j = 0; j < map.Width; j++)
                {
                    grid = new Grid();
                    tb = new TextBlock();
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.Text = "";
                    grid.Children.Add(tb);
                    grid_map.Children.Add(grid);
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, j);
                }
            }
            for (i = 0; i < map.SnakeCount; i++)
            {
                (grid_Connection.Children[3 * i + 2] as Button).IsEnabled = true;
                playerStatus[i] = PlayerStatus.Playing;
            }

            Dictionary<Coord, MapType> changes = map.GetChanges();
            UpdateMap(changes);
            byte[] buff = new byte[1] { (byte)lib.DataType.MapData }.Concat(GetBuffer(changes)).ToArray();
            server.SendToAllAsync(buff).Wait();

            timer = new System.Timers.Timer(200);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dictionary<Coord, MapType> changes = null;
            map.MoveNext();
            changes = map.GetChanges();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateMap(changes);
            }));
            if (changes.Count > 0)
            {
                byte[] buff = new byte[1] { (byte)lib.DataType.MapData }.Concat(GetBuffer(changes)).ToArray();
                server.SendToAllAsync(buff);
            }
            List<byte> died = new List<byte>();
            int liveCount = 0, liveIndex = -1;
            for (int i = 0; i < map.SnakeCount; i++)
            {
                if (map.NextStatus[i] == MapType.HiddenSuperFruitAsShortenFruit)
                {
                    ServerNotification sn = new ServerNotification()
                    {
                        Index = i,
                        ElseMessage = string.Format("赞! 给玩家{0}点赞, 吃到了伪装过后的炒鸡果", i + 1),
                        PlayerMessage = string.Format("66666")
                    };
                    server.SendToAllAsync(sn, 1).Wait();
                }
                else if (map.NextStatus[i] == MapType.HiddenSuperShortenFruitAsSuperFruit)
                {
                    ServerNotification sn = new ServerNotification()
                    {
                        Index = i,
                        ElseMessage = string.Format("哈哈 让我们一起来蛤玩家{0}, 吃到了假的炒鸡果", i + 1),
                        PlayerMessage = string.Format("吃到假货了吧 蛤你!")
                    };
                    server.SendToAllAsync(sn, 1).Wait();
                }
                else if (map.NextStatus[i] == MapType.BlindFruit || map.NextStatus[i] == MapType.RushFruit || map.NextStatus[i] == MapType.SlowFruit || map.NextStatus[i] == MapType.HardFruit)
                {
                    if (map.Snakes[i].Inventory.ContainsKey(map.NextStatus[i]))
                        clients[i].SendAsync(new byte[]
                        {
                            (byte)DataType.AddInventory,
                            (byte)map.NextStatus[i],
                            map.Snakes[i].Inventory[map.NextStatus[i]]
                        }).Wait();
                }

                if (playerStatus[i] == PlayerStatus.Playing)
                {
                    liveCount++;
                    liveIndex = i;
                }
                if (playerStatus[i] == PlayerStatus.Playing && map.RunSnakes[i] == false)
                {
                    died.Add((byte)i);
                    playerStatus[i] = PlayerStatus.Died;
                }
            }
            if (died.Count > 0)
            {
                byte[] buff = new byte[] { (byte)lib.DataType.SnakeDied, (byte)died.Count }.Concat(died).ToArray();
                server.SendToAllAsync(buff).Wait();
            }
            if (liveCount == 1 && map.SnakeCount > 1)
            {
                server.SendToAllAsync(new byte[] { (byte)lib.DataType.GameEnd, (byte)liveIndex });
                timer.Enabled = false;
            }
        }

        private byte[] GetBuffer(Dictionary<Coord, MapType> dic)
        {
            int count = dic.Count;
            IEnumerable<byte> bytes = BitConverter.GetBytes(count);
            for (int i = 0; i < count; i++)
            {
                var pair = dic.ElementAt(i);
                bytes = bytes.Concat(BitConverter.GetBytes(pair.Key.X))
                             .Concat(BitConverter.GetBytes(pair.Key.Y))
                             .Concat(new byte[1] { (byte)pair.Value });
            }
            for (int i = 0; i < map.SnakeCount; i++)
            {
                bytes = bytes.Concat(BitConverter.GetBytes(map.Snakes[i].Length));
            }
            return bytes.ToArray();
        }
    }
}
