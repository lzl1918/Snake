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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SnakeAI
{
    public partial class MainWindow : Window
    {
        static SolidColorBrush FruitBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush SnakeBrush = new SolidColorBrush(ColorUtil.GetRandomColor());
        static SolidColorBrush TransparentBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        int width = 60;
        int height = 60;

        Map map = null;

        System.Timers.Timer timer = null;

        List<Coord> path = new List<Coord>();
        Coord oldTail = null;


        public MainWindow()
        {
            InitializeComponent();

            btn_start.Click += Btn_start_Click;
        }

        private void Btn_start_Click(object sender, RoutedEventArgs e)
        {
            OnStart();
        }

        private void OnStart()
        {
            map = new Map(width, height);

            int i = 0, j = 0;
            Grid grid;
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
                    grid_map.Children.Add(grid);
                    grid.Margin = new Thickness(1);
                    Grid.SetRow(grid, i);
                    Grid.SetColumn(grid, j);
                }
            }

            UpdateMap(map.GetChanges());

            timer = new System.Timers.Timer(10);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
        }

        
        private async void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool findFruit = false;
            bool findTail = false;
            List<Coord> pathFruit = null, pathTail = null, path = null;
            int[,] distTail = null;
            
            pathFruit = map.BFS(map.Fruit);
            if (pathFruit != null)
            {
                Map cmap = map.CopyMove(pathFruit);
                pathTail = cmap.BFS(cmap.Snake.Tail);
                if (pathTail != null)
                {
                    path = pathFruit;
                    path.RemoveAt(0);
                }
            }
            Coord crd = null;
            if (path == null)
            {
                distTail = map.BFS(map.Snake.Tail, out findTail);
                crd = Map.GetMaxium(distTail, map.Snake.Head);
                if (crd != null)
                    path = new List<Coord>() { map.Snake.Head + crd };
            }
            if (path == null && crd == null)
            {
                var cdir = map.Snake.Coords.First.Next.Value - map.Snake.Coords.First.Value;
                if (map[map.Snake.Head + Coord.Up] == MapType.Empty && (map[map.Snake.Head + Coord.Up + Coord.Left] == MapType.Empty || map[map.Snake.Head + Coord.Up + Coord.Right] == MapType.Empty) && cdir != Coord.Down)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Up };
                }
                else if ((map[map.Snake.Head + Coord.Up] != MapType.Empty || (map[map.Snake.Head + Coord.Up + Coord.Left] != MapType.Empty && map[map.Snake.Head + Coord.Up + Coord.Right] != MapType.Empty)) && map[map.Snake.Head + Coord.Left] == MapType.Empty && cdir != Coord.Right)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Left };
                }
                else if ((map[map.Snake.Head + Coord.Up] != MapType.Empty || (map[map.Snake.Head + Coord.Up + Coord.Left] != MapType.Empty && map[map.Snake.Head + Coord.Up + Coord.Right] != MapType.Empty)) && map[map.Snake.Head + Coord.Right] == MapType.Empty && cdir != Coord.Left)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Right };
                }

                else if (map[map.Snake.Head + Coord.Down] == MapType.Empty && (map[map.Snake.Head + Coord.Down + Coord.Left] == MapType.Empty || map[map.Snake.Head + Coord.Down + Coord.Right] == MapType.Empty) && cdir != Coord.Up)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Down };
                }
                else if ((map[map.Snake.Head + Coord.Down] != MapType.Empty || (map[map.Snake.Head + Coord.Down + Coord.Left] != MapType.Empty && map[map.Snake.Head + Coord.Down + Coord.Right] != MapType.Empty)) && map[map.Snake.Head + Coord.Left] == MapType.Empty && cdir != Coord.Right)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Left };
                }
                else if ((map[map.Snake.Head + Coord.Down] != MapType.Empty || (map[map.Snake.Head + Coord.Down + Coord.Left] != MapType.Empty && map[map.Snake.Head + Coord.Down + Coord.Right] != MapType.Empty)) && map[map.Snake.Head + Coord.Right] == MapType.Empty && cdir != Coord.Left)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Right };
                }

                else if (map[map.Snake.Head + Coord.Left] == MapType.Empty && (map[map.Snake.Head + Coord.Left + Coord.Up] == MapType.Empty || map[map.Snake.Head + Coord.Left + Coord.Down] == MapType.Empty) && cdir != Coord.Right)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Left };
                }
                else if ((map[map.Snake.Head + Coord.Left] != MapType.Empty || (map[map.Snake.Head + Coord.Left + Coord.Up] != MapType.Empty && map[map.Snake.Head + Coord.Left + Coord.Down] != MapType.Empty)) && map[map.Snake.Head + Coord.Up] == MapType.Empty && cdir != Coord.Down)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Up };
                }
                else if ((map[map.Snake.Head + Coord.Left] != MapType.Empty || (map[map.Snake.Head + Coord.Left + Coord.Up] != MapType.Empty && map[map.Snake.Head + Coord.Left + Coord.Down] != MapType.Empty)) && map[map.Snake.Head + Coord.Down] == MapType.Empty && cdir != Coord.Up)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Down };
                }

                else if (map[map.Snake.Head + Coord.Right] == MapType.Empty && (map[map.Snake.Head + Coord.Right + Coord.Up] == MapType.Empty || map[map.Snake.Head + Coord.Right + Coord.Down] == MapType.Empty) && cdir != Coord.Left)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Right };
                }
                else if ((map[map.Snake.Head + Coord.Right] != MapType.Empty || (map[map.Snake.Head + Coord.Right + Coord.Up] != MapType.Empty && map[map.Snake.Head + Coord.Right + Coord.Down] != MapType.Empty)) && map[map.Snake.Head + Coord.Up] == MapType.Empty && cdir != Coord.Down)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Up };
                }
                else if ((map[map.Snake.Head + Coord.Right] != MapType.Empty || (map[map.Snake.Head + Coord.Right + Coord.Up] != MapType.Empty && map[map.Snake.Head + Coord.Right + Coord.Down] != MapType.Empty)) && map[map.Snake.Head + Coord.Down] == MapType.Empty && cdir != Coord.Up)
                {
                    path = new List<Coord>() { map.Snake.Head + Coord.Down };
                }

            }

            Coord direction = path[0] - map.Snake.Head;
            map.Snake.Direction = direction;
            path.RemoveAt(0);
            oldTail = new Coord(map.Snake.Tail.X, map.Snake.Tail.Y);
            map.MoveNext();
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                tb_len.Text = map.Snake.Length.ToString();
                UpdateMap(map.GetChanges());
            }));

        }

        private void UpdateMap(IEnumerable<KeyValuePair<Coord, MapType>> changes)
        {
            foreach (var item in changes)
            {
                Grid grid = grid_map.Children[item.Key.Y * map.Width + item.Key.X] as Grid;
                if (item.Value == MapType.Empty)
                    grid.Background = TransparentBrush;

                else if (item.Value == MapType.Fruit)
                    grid.Background = FruitBrush;
                else if (item.Value == MapType.Snake)
                    grid.Background = SnakeBrush;
            }
        }
    }
}
