using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using lib.Media;
using System.Threading;
using System.IO;

namespace SnakeClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static SolidColorBrush StaticFruitBrush = new SolidColorBrush(Color.FromRgb(30, 100, 30));
        static SolidColorBrush StaticShortenFruitBrush = new SolidColorBrush(Color.FromRgb(100, 30, 30));
        static SolidColorBrush StaticToolFruitBrush = new SolidColorBrush(Color.FromRgb(30, 30, 100));

        static SolidColorBrush FruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush BindFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush RushFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush SlowFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush HardFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush SuperFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush ShortenFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush SuperShortenFruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush[] SnakeBrushs = null;
        static SolidColorBrush[] SnakeHeadBrushs = null;
        static SolidColorBrush DieBrush = new SolidColorBrush(Color.FromArgb(100, 40, 10, 10));
        static SolidColorBrush DisconnectBrush = new SolidColorBrush(Color.FromArgb(100, 100, 100, 100));
        static SolidColorBrush TransparentBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        static SolidColorBrush WhiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        static SolidColorBrush ToolSelectedBrush = new SolidColorBrush(Color.FromArgb(100, 100, 100, 200));

        byte playerIndex = 0;
        byte playerCount = 0;
        int[] lengthes = null;
        bool[] snakeDied = null;
        bool isColorfulMode = true;
        lib.Coord[] snakeHeads = null;
        lib.MapType[,] mapdata = null;
        bool[] snakeHard = null;
        string[] snakeName = null;

        object locker = new object();

        Timer notificationHideTimer = null;
        Timer blindTimer = null;
        int selectedTool = -1;
        bool isgamestarted = false;
        lib.Net.Sockets.SocketClient client = null;
        int stageWidth, stageHeight;
        System.Timers.Timer pingtimer = null;
        List<KeyValuePair<lib.MapType, byte>> tools = new List<KeyValuePair<lib.MapType, byte>>(5)
        {
            new KeyValuePair<lib.MapType, byte>(lib.MapType.Empty, 0),
            new KeyValuePair<lib.MapType, byte>(lib.MapType.Empty, 0),
            new KeyValuePair<lib.MapType, byte>(lib.MapType.Empty, 0),
            new KeyValuePair<lib.MapType, byte>(lib.MapType.Empty, 0),
            new KeyValuePair<lib.MapType, byte>(lib.MapType.Empty, 0),
        };
        public MainWindow()
        {
            InitializeComponent();

            btn_connect.Click += Btn_connect_Click;
            btn_prepare.Click += Btn_prepare_Click;
            btn_preset.Click += Btn_preset_Click;

            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            ReadIPText();
        }

        private void ReadIPText()
        {
            if(File.Exists("ips.txt"))
            {
                FileStream stream = File.Open("ips.txt", FileMode.Open);
                StreamReader reader = new StreamReader(stream);
                string iptext = reader.ReadLine();
                string port = reader.ReadLine();
                reader.Dispose();
                stream.Dispose();

                tb_ipaddr.Text = iptext;
                tb_port.Text = port;
            }
        }
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (isgamestarted == false)
                return;
            lib.Coord crd = null;

            switch (e.Key)
            {
                case Key.W:
                    crd = new lib.Coord(0, -1);
                    break;
                case Key.A:
                    crd = new lib.Coord(-1, 0);
                    break;
                case Key.S:
                    crd = new lib.Coord(0, 1);
                    break;
                case Key.D:
                    crd = new lib.Coord(1, 0);
                    break;
            }
            if (crd != null)
            {
                byte[] data = new byte[3] { (byte)lib.DataType.ChangeDirection, (byte)(crd.X + 1), (byte)(crd.Y + 1) };
                client.SendAsync(data);
                return;
            }

            if (e.Key == Key.Space)
            {
                if (selectedTool != -1)
                    (grid_inventory.Children[selectedTool] as Grid).Background = TransparentBrush;
                selectedTool += 1;
                if (selectedTool == 5)
                    selectedTool = -1;
                if (selectedTool != -1)
                {
                    if (tools[selectedTool].Key != lib.MapType.Empty)
                        (grid_inventory.Children[selectedTool] as Grid).Background = ToolSelectedBrush;
                    else
                        selectedTool = -1;
                }
            }
            else if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                if (selectedTool == -1 || tools[selectedTool].Key == lib.MapType.Empty)
                    ShowNotification(new lib.ServerNotification()
                    {
                        Index = playerIndex,
                        PlayerMessage = "选择一个道具"
                    });
                else
                {
                    byte player = (byte)(e.Key - Key.D1);
                    if (player >= playerCount)
                    {
                        ShowNotification(new lib.ServerNotification()
                        {
                            Index = playerIndex,
                            PlayerMessage = "选择一个有效的玩家"
                        });
                    }
                    else
                        client.SendAsync(new byte[] { (byte)lib.DataType.UseInventory, (byte)tools[selectedTool].Key, player });
                }
            }
            e.Handled = true;
        }

        private void Client_Disconnected(object sender, lib.Net.Sockets.SocketDisconnectedEventArgs args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                btn_prepare.IsEnabled = false;
                btn_connect.IsEnabled = true;
            }));
            MessageBox.Show("连接关闭");
            client.Dispose();
            client = null;
        }

        private void Btn_preset_Click(object sender, RoutedEventArgs e)
        {
            if (tb_ipaddr.Text == "127.0.0.1")
            {
                tb_ipaddr.Text = "godliao.imwork.net";
                tb_port.Text = "27758";
            }
            else
            {
                tb_ipaddr.Text = "127.0.0.1";
                tb_port.Text = "12345";
            }
        }

        private void Client_MessageReceived(lib.Net.Sockets.SocketClient socket, lib.Net.Sockets.SocketMessageEventArgs args)
        {
            foreach (var msg in args.Messages)
            {
                int offset = 0;
                #region DataMessage
                if (msg.MsgType == lib.Net.Sockets.MessageType.Data)
                {
                    switch ((lib.DataType)msg.DataMessage.Data[0])
                    {
                        #region PlayerName
                        case lib.DataType.SetName:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                byte tindex = msg.DataMessage.Data[1];
                                string name = Encoding.UTF8.GetString(msg.DataMessage.Data, 2, (int)msg.DataMessage.Length - 2);
                                snakeName[tindex] = name;
                                if (tindex != playerIndex)
                                {
                                    (((sv_playersLayer.Content as StackPanel).Children[tindex] as Grid).Children[0] as TextBlock).Text = name;
                                    (grid_gameInfo.Children[tindex] as TextBlock).Text = string.Format("{0}: {1}", name, lengthes[tindex]);
                                }
                            }));
                            break;
                        #endregion

                        #region Inventory
                        case lib.DataType.UseInventory:
                            lib.MapType inventory = (lib.MapType)msg.DataMessage.Data[1];
                            switch (inventory)
                            {
                                case lib.MapType.BlindFruit:
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        ShowBlind();
                                    }));
                                    break;
                            }
                            break;

                        case lib.DataType.AddInventory:
                            int toolindex = 0;
                            byte amount = msg.DataMessage.Data[2];
                            while (toolindex < 5 && tools[toolindex].Key != lib.MapType.Empty && tools[toolindex].Key != (lib.MapType)msg.DataMessage.Data[1])
                                toolindex++;
                            if (toolindex == 5)
                                return;
                            if (amount == 0)
                            {
                                tools.RemoveAt(toolindex);
                                tools.Add(new KeyValuePair<lib.MapType, byte>(lib.MapType.Empty, 0));
                            }
                            else
                            {
                                tools[toolindex] = new KeyValuePair<lib.MapType, byte>((lib.MapType)msg.DataMessage.Data[1], amount);
                            }
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                UpdateInventory();
                            }));
                            break;
                        #endregion

                        #region HardChanged
                        case lib.DataType.HardChanged:
                            byte toplayer = msg.DataMessage.Data[1];
                            bool isHard = msg.DataMessage.Data[2] == 1;
                            this.snakeHard[toplayer] = isHard;
                            break;
                        #endregion

                        #region Disconnected
                        case lib.DataType.PlayerDisconnected:
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                byte index = msg.DataMessage.Data[1];
                                snakeDied[index] = true;
                                TextBlock infotb = grid_gameInfo.Children[index] as TextBlock;
                                infotb.Text = string.Format("{0}: 在火星", snakeName[index]);
                            }));
                            break;
                        #endregion

                        #region GameEnd
                        case lib.DataType.GameEnd:
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameEnd(msg.DataMessage.Data[1]);
                            }));
                            break;
                        #endregion

                        #region SnakeDied
                        case lib.DataType.SnakeDied:
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ShowSnakeDied(msg.DataMessage.Data);
                            }));
                            break;
                        #endregion

                        #region PlayerIndex
                        case lib.DataType.PlayerIndex:
                            playerIndex = msg.DataMessage.Data[1];
                            break;
                        #endregion

                        #region PingTest
                        case lib.DataType.PingTest:
                            int val = BitConverter.ToInt32(msg.DataMessage.Data, 1);
                            int sv = DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
                            if (sv < val)
                                sv += 60 * 1000;
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                this.Title = string.Format("ping: {0}", sv - val);
                            }));
                            break;
                        #endregion

                        #region PlayerStatus
                        case lib.DataType.PlayerStatus:
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ShowPlayerStatus(msg.DataMessage.Data);
                            }));
                            break;
                        #endregion

                        #region GameStart
                        case lib.DataType.GameStart:
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                StartGame();
                            }));
                            break;
                        #endregion

                        #region StageSizeQuery
                        case lib.DataType.StageSizeQuery:
                            stageHeight = BitConverter.ToInt32(msg.DataMessage.Data, 1);
                            stageWidth = BitConverter.ToInt32(msg.DataMessage.Data, 5);
                            break;
                        #endregion

                        #region MapData
                        case lib.DataType.MapData:
                            offset = 1;
                            int len = BitConverter.ToInt32(msg.DataMessage.Data, offset);
                            offset += 4;
                            short x, y;
                            Dictionary<lib.Coord, lib.MapType> dic = new Dictionary<lib.Coord, lib.MapType>();
                            for (int i = 0; i < len; i++)
                            {
                                x = BitConverter.ToInt16(msg.DataMessage.Data, offset);
                                offset += 2;
                                y = BitConverter.ToInt16(msg.DataMessage.Data, offset);
                                offset += 2;
                                dic.Add(new lib.Coord(x, y), (lib.MapType)msg.DataMessage.Data[offset]);
                                offset += 1;
                            }
                            for (int i = 0; i < playerCount; i++)
                            {
                                lengthes[i] = BitConverter.ToInt32(msg.DataMessage.Data, offset);
                                offset += 4;
                            }
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                EditMap(dic);
                            }));

                            break;
                            #endregion
                    }
                }
                #endregion

                #region ObjectMessage
                else if (msg.MsgType == lib.Net.Sockets.MessageType.Object)
                {
                    object obj = msg.ObjectMessage.Object;
                    if (obj.GetType() == typeof(lib.ServerNotification))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ShowNotification(obj as lib.ServerNotification);
                        }));
                    }
                }
                #endregion
            }
        }

        private void StartGame()
        {
            if (isgamestarted == true)
                return;

            if ((sv_playersLayer.Parent as Grid).Visibility == Visibility.Visible)
            {
                DoubleAnimation da = new DoubleAnimation()
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                da.Completed += (sender, e) =>
                {
                    (sv_playersLayer.Parent as Grid).Visibility = Visibility.Collapsed;
                };
                (sv_playersLayer.Parent as Grid).BeginAnimation(Grid.OpacityProperty, da);
                DoubleAnimation transformDa = new DoubleAnimation()
                {
                    To = 0 - sv_topBar.Height,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                (sp_topBarContainer.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, transformDa);
            }

            isColorfulMode = rb_colorfulMode.IsChecked == true;
            isgamestarted = true;
            mapdata = new lib.MapType[stageHeight, stageWidth];
            int i = 0, j = 0;
            Grid grid;
            TextBlock tb;
            grid_map.Children.Clear();
            grid_map.RowDefinitions.Clear();
            grid_map.ColumnDefinitions.Clear();
            for (i = 0; i < stageWidth; i++)
                grid_map.ColumnDefinitions.Add(new ColumnDefinition());
            for (j = 0; j < stageHeight; j++)
                grid_map.RowDefinitions.Add(new RowDefinition());
            for (i = 0; i < stageHeight; i++)
            {
                for (j = 0; j < stageWidth; j++)
                {
                    grid = new Grid();
                    tb = new TextBlock();
                    tb.VerticalAlignment = VerticalAlignment.Center;
                    tb.HorizontalAlignment = HorizontalAlignment.Center;
                    tb.Foreground = WhiteBrush;
                    grid.Children.Add(tb);
                    grid_map.Children.Add(grid);
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, j);
                }
            }
        }

        private void EditMap(Dictionary<lib.Coord, lib.MapType> changes)
        {
            lock (locker)
            {
                foreach (var item in changes)
                {
                    Grid grid = grid_map.Children[item.Key.Y * stageWidth + item.Key.X] as Grid;
                    if (mapdata[item.Key.Y, item.Key.X] <= lib.MapType.SnakeHeadStart + playerCount - 1 && mapdata[item.Key.Y, item.Key.X] >= lib.MapType.SnakeHeadStart)
                    {
                        if (snakeHard[mapdata[item.Key.Y, item.Key.X] - lib.MapType.SnakeHeadStart] == true)
                            (grid.Children[0] as TextBlock).Text = "";
                    }

                    if (item.Value == lib.MapType.Empty)
                    {
                        grid.Background = TransparentBrush;
                        (grid.Children[0] as TextBlock).Text = "";
                    }
                    #region 功能性水果
                    else if (item.Value == lib.MapType.BlindFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = BindFruitBrush;
                        else
                        {
                            grid.Background = StaticToolFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "B";
                        }
                    }
                    else if (item.Value == lib.MapType.RushFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = RushFruitBrush;
                        else
                        {
                            grid.Background = StaticToolFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "R";
                        }
                    }
                    else if (item.Value == lib.MapType.SlowFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = SlowFruitBrush;
                        else
                        {
                            grid.Background = StaticToolFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "S";
                        }
                    }
                    else if (item.Value == lib.MapType.HardFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = HardFruitBrush;
                        else
                        {
                            grid.Background = StaticToolFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "H";
                        }
                    }
                    #endregion

                    #region 可食用水果
                    else if (item.Value == lib.MapType.Fruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = FruitBrush;
                        else
                        {
                            grid.Background = StaticFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "1";
                        }
                    }
                    else if (item.Value == lib.MapType.SuperFruit || item.Value == lib.MapType.HiddenSuperShortenFruitAsSuperFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = SuperFruitBrush;
                        else
                        {
                            grid.Background = StaticFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "5";
                        }
                    }

                    else if (item.Value == lib.MapType.ShortenFruit || item.Value == lib.MapType.HiddenSuperFruitAsShortenFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = ShortenFruitBrush;
                        else
                        {
                            grid.Background = StaticShortenFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "1";
                        }
                    }
                    else if (item.Value == lib.MapType.SuperShortenFruit)
                    {
                        if (isColorfulMode == true)
                            grid.Background = SuperShortenFruitBrush;
                        else
                        {
                            grid.Background = StaticShortenFruitBrush;
                            (grid.Children[0] as TextBlock).Text = "5";
                        }
                    }
                    #endregion

                    else if (item.Value <= lib.MapType.SnakeStart + playerCount - 1 && item.Value >= lib.MapType.SnakeStart)
                    {
                        grid.Background = SnakeBrushs[item.Value - lib.MapType.SnakeStart];
                        (grid.Children[0] as TextBlock).Text = "";
                    }
                    else if (item.Value <= lib.MapType.SnakeHeadStart + playerCount - 1 && item.Value >= lib.MapType.SnakeHeadStart)
                    {
                        grid.Background = SnakeHeadBrushs[item.Value - lib.MapType.SnakeHeadStart];
                        snakeHeads[item.Value - lib.MapType.SnakeHeadStart] = new lib.Coord(item.Key.X, item.Key.Y);
                        if (snakeHard[item.Value - lib.MapType.SnakeHeadStart] == true)
                            (grid.Children[0] as TextBlock).Text = "h";
                        else
                            (grid.Children[0] as TextBlock).Text = "";
                    }

                    mapdata[item.Key.Y, item.Key.X] = item.Value;
                }
                for (int i = 0; i < playerCount; i++)
                {
                    if (snakeDied[i] == true)
                        continue;

                    TextBlock infotb = grid_gameInfo.Children[i] as TextBlock;
                    if (i == playerIndex)
                        infotb.Text = string.Format("{0} (你): {1}",snakeName[i], lengthes[i]);
                    else
                        infotb.Text = string.Format("{0}: {1}", snakeName[i], lengthes[i]);
                }
            }
        }

        private void Btn_prepare_Click(object sender, RoutedEventArgs e)
        {
            byte[] data = new byte[1] { (byte)lib.DataType.Prepared };
            client.SendAsync(data);
            btn_prepare.IsEnabled = false;
        }

        private void Btn_connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (client == null)
                {
                    client = new lib.Net.Sockets.SocketClient();
                    client.MessageReceived += Client_MessageReceived;
                    client.Disconnected += Client_Disconnected;
                }
                client.ConnectAsync(tb_ipaddr.Text, int.Parse(tb_port.Text)).Wait();

                btn_connect.IsEnabled = false;
                btn_prepare.IsEnabled = true;

                StartPingTest();
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法连接");
            }
        }

        private void StartPingTest()
        {
            if (pingtimer == null)
            {
                pingtimer = new System.Timers.Timer(1000);
                pingtimer.Elapsed += Pingtimer_Elapsed;
                pingtimer.AutoReset = true;
                pingtimer.Enabled = true;
            }
            else
            {
                //pingtimer.Enabled = true;
            }
        }
        private void EndPingTest()
        {
            if (pingtimer != null)
            {
                pingtimer.Dispose();
            }
        }
        private void Pingtimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int val = DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
            byte[] bytes = new byte[1] { (byte)lib.DataType.PingTest }.Concat(BitConverter.GetBytes(val)).ToArray();
            client.SendAsync(bytes).Wait();
        }

        private void ShowPlayerStatus(byte[] data)
        {
            playerCount = data[1];
            int i;
            if (SnakeBrushs == null)
            {
                snakeHeads = new lib.Coord[playerCount];
                SnakeBrushs = new SolidColorBrush[playerCount];
                SnakeHeadBrushs = new SolidColorBrush[playerCount];
                snakeDied = new bool[playerCount];
                snakeHard = new bool[playerCount];
                lengthes = new int[playerCount];
                snakeName = new string[playerCount];
                for (i = 0; i < playerCount; i++)
                {
                    SnakeBrushs[i] = new SolidColorBrush(ColorUtil.GetRandomColor());
                    SnakeHeadBrushs[i] = new SolidColorBrush(SnakeBrushs[i].Color.GetDarker());
                    snakeName[i] = "辣鸡";
                }
                snakeName[playerIndex] = Properties.Settings.Default.lastName;
                byte[] buff = new byte[] { (byte)lib.DataType.SetName, playerIndex }.Concat(Encoding.UTF8.GetBytes(Properties.Settings.Default.lastName)).ToArray();
                client.SendAsync(buff).Wait();

            }

            if (sv_playersLayer.Opacity == 0)
            {
                for (i = 0; i < playerCount; i++)
                {
                    Grid g = (this.Resources["userTemplate"] as DataTemplate).LoadContent() as Grid;
                    (g.Children[0] as TextBlock).Text = snakeName[i];
                    if (i == playerIndex)
                    {
                        TextBox tb = new TextBox();
                        tb.Opacity = 0;
                        tb.Visibility = Visibility.Collapsed;
                        tb.FontSize = 22;
                        g.Children.Insert(0, tb);
                        tb.Text = snakeName[playerIndex];
                        tb.KeyUp += Tb_EditName_KeyUp;
                        g.MouseUp += Grid_Players_MouseUp;
                    }
                    (sv_playersLayer.Content as StackPanel).Children.Add(g);
                }
                DoubleAnimation da = new DoubleAnimation()
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                sv_playersLayer.BeginAnimation(StackPanel.OpacityProperty, da);

                grid_gameInfo.Children.Clear();
                grid_gameInfo.ColumnDefinitions.Clear();
                for (i = 0; i < playerCount; i++)
                {
                    grid_gameInfo.ColumnDefinitions.Add(new ColumnDefinition());
                    TextBlock infotb = new TextBlock();
                    if (i == playerIndex)
                        infotb.Text = string.Format("{0} (你): {1}", snakeName[i], lengthes[i]);
                    else
                        infotb.Text = string.Format("{0}: {1}", snakeName[i], lengthes[i]);
                    infotb.VerticalAlignment = VerticalAlignment.Center;
                    grid_gameInfo.Children.Add(infotb);
                    Grid.SetColumn(infotb, i);
                }
            }

            for (i = 0; i < playerCount; i++)
            {
                TextBlock tb = ((sv_playersLayer.Content as StackPanel).Children[i] as Grid).Children[i == playerIndex ? 2 : 1] as TextBlock;
                switch (data[2 + i])
                {
                    case 0:
                        tb.Text = "未连接";
                        break;
                    case 1:
                        tb.Text = "连接成功";
                        break;
                    case 2:
                        tb.Text = "准备完毕";
                        break;
                }
            }
        }

        private void Tb_EditName_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ShowEditNameTextBox(false, (sender as TextBox).Parent as Grid);

                string name = (sender as TextBox).Text;
                (((sender as TextBox).Parent as Grid).Children[1] as TextBlock).Text = name;
                (grid_gameInfo.Children[playerIndex] as TextBlock).Text = string.Format("{0} (你): {1}", name, lengthes[playerIndex]);
                byte[] buff = new byte[] { (byte)lib.DataType.SetName, playerIndex }.Concat(Encoding.UTF8.GetBytes(name)).ToArray();
                client.SendAsync(buff).Wait();
                Properties.Settings.Default.lastName = name;
                Properties.Settings.Default.Save();
            }
        }
        private void Grid_Players_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Grid g = sender as Grid;
            if (g.Children[0].Visibility == Visibility.Collapsed)
                ShowEditNameTextBox(true, g);
        }
        private void ShowEditNameTextBox(bool show, Grid g)
        {
            if (show == true && g.Children[0].Visibility == Visibility.Collapsed)
                g.Children[0].Visibility = Visibility.Visible;
            else if (show == false && g.Children[1].Visibility == Visibility.Collapsed)
            {
                this.Focus();
                g.Children[1].Visibility = Visibility.Visible;
            }

            DoubleAnimation da = new DoubleAnimation()
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500)
            },
            da1 = new DoubleAnimation()
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            da1.Completed += (s1, e1) =>
            {
                if (show == false)
                    g.Children[0].Visibility = Visibility.Collapsed;
                if (show == true)
                {
                    g.Children[0].Focus();
                    g.Children[1].Visibility = Visibility.Collapsed;
                }
            };

            g.Children[show ? 0 : 1].BeginAnimation(OpacityProperty, da);
            g.Children[show ? 1 : 0].BeginAnimation(OpacityProperty, da1);
        }


        private void ShowSnakeDied(byte[] data)
        {
            byte i = 0;
            for (i = 0; i < data[1]; i++)
            {
                if (data[2 + i] == playerIndex)
                {
                    grid_dieMask.Visibility = Visibility.Visible;
                    TextBlock infotb = grid_gameInfo.Children[data[2 + i]] as TextBlock;
                    infotb.Text = string.Format("你: pogai");
                }
                else
                {
                    TextBlock infotb = grid_gameInfo.Children[data[2 + i]] as TextBlock;
                    infotb.Text = string.Format("玩家{0}: pogai", data[2 + i] + 1);
                }
                snakeDied[data[2 + i]] = true;
            }
        }

        private void GameEnd(byte index)
        {
            if (index == playerIndex)
            {
                grid_youWin.Visibility = Visibility.Visible;
            }
            else
            {
                grid_dieMask.Visibility = Visibility.Collapsed;
                (grid_elseWin.Children[0] as TextBlock).Text = string.Format("wow, {0}号好流弊啊", index + 1);
                grid_elseWin.Visibility = Visibility.Visible;
            }
        }

        private void ShowNotification(lib.ServerNotification sn)
        {
            if (notificationHideTimer == null)
            {
                DoubleAnimation da = new DoubleAnimation()
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(500)
                };
                (grid_notification.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, da);
            }
            if (sn.Index == playerIndex)
                lv_notifications.Items.Add(sn.PlayerMessage);
            else
                lv_notifications.Items.Add(sn.ElseMessage);
            lv_notifications.ScrollIntoView(lv_notifications.Items[lv_notifications.Items.Count - 1]);

            if (notificationHideTimer == null)
            {
                notificationHideTimer = new Timer(NotificationTimerTicked, null, 5000, Timeout.Infinite);
            }
            else
            {
                notificationHideTimer.Change(5000, Timeout.Infinite);
            }
        }
        private void NotificationTimerTicked(object state)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                notificationHideTimer.Dispose();
                notificationHideTimer = null;
                HideNotification();
            }));
        }
        private void HideNotification()
        {
            DoubleAnimation da = new DoubleAnimation()
            {
                To = 0 - grid_notification.Height,
                Duration = TimeSpan.FromMilliseconds(500)
            };
            da.Completed += (sender, e) =>
            {
                lv_notifications.Items.Clear();
            };
            (grid_notification.RenderTransform as TranslateTransform).BeginAnimation(TranslateTransform.YProperty, da);
        }

        private void UpdateInventory()
        {
            for (int i = 0; i < 5; i++)
            {
                TextBlock tb = (grid_inventory.Children[i] as Grid).Children[0] as TextBlock;
                switch (tools[i].Key)
                {
                    case lib.MapType.Empty:
                        tb.Text = "无道具";
                        break;
                    case lib.MapType.BlindFruit:
                        tb.Text = string.Format("致盲果 x{0}", tools[i].Value);
                        break;
                    case lib.MapType.RushFruit:
                        tb.Text = string.Format("跑快快 x{0}", tools[i].Value);
                        break;
                    case lib.MapType.SlowFruit:
                        tb.Text = string.Format("蜗牛果 x{0}", tools[i].Value);
                        break;
                    case lib.MapType.HardFruit:
                        tb.Text = string.Format("啊好硬 x{0}", tools[i].Value);
                        break;
                }
            }
        }

        private void ShowBlind()
        {
            if (blindTimer == null)
            {
                DoubleAnimation da = new DoubleAnimation()
                {
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(1000)
                };
                grid_Blind.Opacity = 0;
                grid_Blind.Visibility = Visibility.Visible;
                grid_Blind.BeginAnimation(Grid.OpacityProperty, da);
                blindTimer = new Timer(BlindTimerTicked, null, 10000, Timeout.Infinite);
            }
            else
                blindTimer.Change(10000, Timeout.Infinite);
        }
        private void BlindTimerTicked(object state)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                HideBlind();
                blindTimer = null;
            }));
        }
        private void HideBlind()
        {
            DoubleAnimation da = new DoubleAnimation()
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(1000)
            };
            da.Completed += (sender, e) =>
            {
                grid_Blind.Visibility = Visibility.Collapsed;
            };
            grid_Blind.BeginAnimation(Grid.OpacityProperty, da);
        }
    }
}
