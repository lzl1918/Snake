using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lib.Extensions;
using lib;
using System.Threading;

namespace SnakeServerWPF
{
    public class Map
    {
        private object locker = new object();

        public int SnakeCount = 4;

        public SnakeItem[] Snakes = null;
        public bool[] RunSnakes = null;
        public MapType[] NextStatus = null;
        public int[] SnakeSpeed = null;
        public int[] SnakeSpeedStep = null;
        public bool[] IsHard = null;
        public string[] SnakeNames = null;
        private Timer[] HardTimer = null;

        public event EventHandler<HardChangedArgs> HardChanged;

        public Dictionary<Coord, int> Fruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> SuperFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> HiddenSuperFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> ShortenFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> SuperShortenFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> HiddenSuperShortenFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> BlindFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> RushFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> SlowFruits = new Dictionary<Coord, int>();
        public Dictionary<Coord, int> HardFruits = new Dictionary<Coord, int>();

        int width, height;
        int fruitclearTime = fruitClearMaxTime;
        const int fruitClearMaxTime = 10;
        MapType[,] mapdata = null;
        Dictionary<Coord, MapType> change = new Dictionary<Coord, MapType>();
        Random rand = new Random();
        List<Timer> timers = new List<Timer>();
        public MapType[,] MapData
        {
            get
            {
                return mapdata;
            }
        }

        public int Width
        {
            get
            {
                return width;
            }
            private set
            {
                width = value;
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
            private set
            {
                height = value;
            }
        }

        public Map(int snakecount, int width, int height)
        {
            if (snakecount > height)
                throw new Exception("蛇太多辣");

            byte i;
            int x = 0, y = 0;
            this.width = width;
            this.height = height;
            mapdata = new MapType[height, width];
            NextStatus = new MapType[snakecount];
            RunSnakes = new bool[snakecount];
            Snakes = new SnakeItem[snakecount];
            SnakeSpeed = new int[snakecount];
            SnakeSpeedStep = new int[snakecount];
            IsHard = new bool[snakecount];
            HardTimer = new Timer[snakecount];
            SnakeNames = new string[snakecount];

            for (i = 0; i < snakecount; i++)
            {
                NextStatus[i] = MapType.Unknow;
                RunSnakes[i] = true;
                SnakeSpeed[i] = 1;
                SnakeNames[i] = "辣鸡";
                SnakeSpeedStep[i] = 1;
                mapdata[y, x] = MapType.SnakeHeadStart + i;
                if (x == 0)
                {
                    Snakes[i] = new SnakeItem(new Coord((short)x, (short)y), new Coord(1, 0));
                    x = width - 1;
                }
                else
                {
                    Snakes[i] = new SnakeItem(new Coord((short)x, (short)y), new Coord(-1, 0));
                    x = 0;
                    y += height / snakecount * 2;
                }
                change.Add(Snakes[i].Head, (MapType.SnakeHeadStart + i));
            }

            SnakeCount = snakecount;
            
            #region 产生食物
            ProduceFruit((int)Math.Sqrt(Math.Sqrt(width * height)), MapType.Fruit);
            ProduceFruit((Fruits.Count + 1) * 3 / 4, MapType.ShortenFruit);
            ProduceFruit(snakecount, MapType.SuperFruit);
            ProduceFruit(snakecount, MapType.SuperShortenFruit);
            ProduceFruit(1, MapType.HiddenSuperShortenFruitAsSuperFruit);
            ProduceFruit(1, MapType.HiddenSuperFruitAsShortenFruit);
            ProduceFruit(snakecount, MapType.RushFruit);
            ProduceFruit(snakecount, MapType.SlowFruit);
            ProduceFruit(snakecount / 2, MapType.HardFruit);
            ProduceFruit(snakecount, MapType.BlindFruit);
            #endregion
        }

        private void EnumNextMoveStatus()
        {
            for (int i = 0; i < SnakeCount; i++)
            {
                EnumNextMoveStatus(i);
            }
        }
        private void EnumNextMoveStatus(int index)
        {
            int tx, ty;
            if (RunSnakes[index] == false)
                return;

            tx = Snakes[index].Coords.First.Value.X + Snakes[index].Direction.X;
            ty = Snakes[index].Coords.First.Value.Y + Snakes[index].Direction.Y;
            if (!(tx >= 0 && tx < width && ty >= 0 && ty < height))
                NextStatus[index] = MapType.Unknow;
            else if (mapdata[ty, tx] >= MapType.SnakeStart && mapdata[ty, tx] <= MapType.SnakeStart + (byte)SnakeCount - 1)
            {
                if (IsHard[mapdata[ty, tx] - MapType.SnakeStart] == true && index != mapdata[ty, tx] - MapType.SnakeStart)
                    NextStatus[index] = MapType.Border;
                else
                    NextStatus[index] = mapdata[ty, tx];
            }
            else if (mapdata[ty, tx] >= MapType.SnakeHeadStart && mapdata[ty, tx] <= MapType.SnakeHeadStart + (byte)SnakeCount - 1)
            {
                if (IsHard[mapdata[ty, tx] - MapType.SnakeHeadStart] == true && index != mapdata[ty, tx] - MapType.SnakeHeadStart)
                    NextStatus[index] = MapType.Border;
                else
                    NextStatus[index] = mapdata[ty, tx];
            }
            else
            {
                NextStatus[index] = mapdata[ty, tx];
            }
        }

        public void MoveNext()
        {
            lock (locker)
            {
                for (byte i = 0; i < SnakeCount; i++)
                {
                    if (RunSnakes[i] == true)
                    {
                        if (SnakeSpeed[i] > 0)
                        {
                            for (byte j = 0; j < SnakeSpeed[i]; j++)
                                MoveSnake(i);
                        }
                        else
                        {
                            if (SnakeSpeedStep[i] == 1)
                            {
                                SnakeSpeedStep[i] = SnakeSpeed[i];
                                MoveSnake(i);
                            }
                            else
                                SnakeSpeedStep[i]++;
                        }
                    }

                    //#region 处理水果
                    //if (fruitclearTime == 0)
                    //{
                    //    int shortenFruitCount = ShortenFruits.Count;
                    //    int superShortenFruitCount = SuperShortenFruits.Count;
                    //    foreach (var fruit in ShortenFruits)
                    //    {
                    //        mapdata[fruit.Key.Y, fruit.Key.X] = MapType.Empty;
                    //        change.Add(fruit.Key, MapType.Empty);
                    //    }
                    //    ShortenFruits.Clear();
                    //    foreach (var fruit in SuperShortenFruits)
                    //    {
                    //        mapdata[fruit.Key.Y, fruit.Key.X] = MapType.Empty;
                    //        change.Add(fruit.Key, MapType.Empty);
                    //    }
                    //    SuperShortenFruits.Clear();
                    //    ProduceFruit(shortenFruitCount, MapType.ShortenFruit);
                    //    ProduceFruit(superShortenFruitCount, MapType.SuperShortenFruit);
                    //    fruitclearTime = fruitClearMaxTime;
                    //}
                    //fruitclearTime--;
                    //#endregion
                }
            }
        }

        public void ChangeSnakeSpeed(int index, int inc)
        {
            SnakeSpeed[index] += inc;
            SpeedChangeData scd = new SpeedChangeData(index, inc);
            Timer t = new Timer((data) =>
            {
                int ind = ((SpeedChangeData)data).Index;
                int sinc = ((SpeedChangeData)data).Inc;
                SnakeSpeed[ind] -= sinc;
                timers.Remove(((SpeedChangeData)data).Timer);
            }, scd, 7000, Timeout.Infinite);
            scd.Timer = t;
            timers.Add(t);
        }

        public void SetSnakeHard(int index)
        {
            IsHard[index] = true;
            if (HardChanged != null)
                HardChanged(this, new HardChangedArgs(index, true));

            if (HardTimer[index] == null)
                HardTimer[index] = new Timer((num) =>
                {
                    int ind = (int)num;
                    IsHard[index] = false;
                    HardTimer[ind] = null;
                    if (HardChanged != null)
                        HardChanged(this, new HardChangedArgs(ind, false));

                }, index, 7000, Timeout.Infinite);
            else
                HardTimer[index].Change(7000, Timeout.Infinite);
        }

        private void MoveSnake(byte i)
        {
            short tailx, taily, headx, heady, nextx, nexty;
            if (RunSnakes[i] == false)
                return;
            EnumNextMoveStatus(i);
            tailx = Snakes[i].Tail.X;
            taily = Snakes[i].Tail.Y;
            headx = Snakes[i].Head.X;
            heady = Snakes[i].Head.Y;
            nextx = (short)(headx + Snakes[i].Direction.X);
            nexty = (short)(heady + Snakes[i].Direction.Y);


            #region 正常移动
            if (NextStatus[i] == MapType.Empty)
            {
                Snakes[i].MoveStep();

                mapdata[taily, tailx] = MapType.Empty;
                change.Add(new Coord(tailx, taily), MapType.Empty);

                mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);
                if (taily != heady || tailx != headx)
                {
                    mapdata[heady, headx] = MapType.SnakeStart + i;
                    change.Add(new Coord(headx, heady), MapType.SnakeStart + i);
                }
            }
            #endregion
            #region 水果
            else if (NextStatus[i] == MapType.Fruit || NextStatus[i] == MapType.SuperFruit || NextStatus[i] == MapType.HiddenSuperFruitAsShortenFruit)
            {
                Snakes[i].IncreaseLen++;
                if (NextStatus[i] == MapType.SuperFruit || NextStatus[i] == MapType.HiddenSuperFruitAsShortenFruit)
                    Snakes[i].IncreaseLen += 5 - 1;
                Snakes[i].MoveStep();

                mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);

                mapdata[heady, headx] = MapType.SnakeStart + i;
                change.Add(new Coord(headx, heady), MapType.SnakeStart + i);

                switch (NextStatus[i])
                {
                    case MapType.Fruit:
                        Fruits.Remove(Snakes[i].Head);
                        break;
                    case MapType.SuperFruit:
                        SuperFruits.Remove(Snakes[i].Head);
                        break;
                    case MapType.HiddenSuperFruitAsShortenFruit:
                        HiddenSuperFruits.Remove(Snakes[i].Head);
                        ProduceFruit(1, MapType.BlindFruit);
                        break;
                }
                ProduceFruit(1, NextStatus[i]);
            }
            else if (NextStatus[i] == MapType.ShortenFruit || NextStatus[i] == MapType.SuperShortenFruit || NextStatus[i] == MapType.HiddenSuperShortenFruitAsSuperFruit)
            {
                int len = Snakes[i].Length - 1;
                if (NextStatus[i] == MapType.SuperShortenFruit || NextStatus[i] == MapType.HiddenSuperShortenFruitAsSuperFruit)
                    len -= 5 - 1;
                if (len <= 0)
                    len = 1;
                ForceShorten(i, len);
                tailx = Snakes[i].Tail.X;
                taily = Snakes[i].Tail.Y;
                Snakes[i].MoveStep();

                mapdata[taily, tailx] = MapType.Empty;
                change.Add(new Coord(tailx, taily), MapType.Empty);

                mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);

                if (taily != heady || tailx != headx)
                {
                    mapdata[heady, headx] = MapType.SnakeStart + i;
                    change.Add(new Coord(headx, heady), MapType.SnakeStart + i);
                }
                switch (NextStatus[i])
                {
                    case MapType.ShortenFruit:
                        ShortenFruits.Remove(Snakes[i].Head);
                        break;
                    case MapType.HiddenSuperShortenFruitAsSuperFruit:
                        HiddenSuperShortenFruits.Remove(Snakes[i].Head);
                        ProduceFruit(1, MapType.BlindFruit);
                        break;
                    case MapType.SuperShortenFruit:
                        SuperShortenFruits.Remove(Snakes[i].Head);
                        break;
                }
                ProduceFruit(1, NextStatus[i]);
            }
            #endregion
            #region 撞到自己
            else if (NextStatus[i] == MapType.SnakeStart + i)
            {
                // 自己撞到自己
                var node = Snakes[i].Coords.First.Next;
                int len = 1;
                while (node != null && (node.Value.X != nextx || node.Value.Y != nexty))
                {
                    node = node.Next;
                    len++;
                }
                if (node == null || node == Snakes[i].Coords.First)
                    return;

                ForceShorten(i, len + 1);
                Snakes[i].MoveStep();

                mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);

                mapdata[heady, headx] = MapType.SnakeStart + i;
                change.Add(new Coord(headx, heady), MapType.SnakeStart + i);
            }
            #endregion
            #region 撞到其它蛇
            else if (NextStatus[i] < MapType.SnakeStart + (byte)SnakeCount && NextStatus[i] >= MapType.SnakeStart)
            {
                // 撞到其它蛇                        
                int ts = NextStatus[i] - MapType.SnakeStart;
                var node = Snakes[ts].Coords.First;
                int len = 0;
                while (node != null && (node.Value.X != nextx || node.Value.Y != nexty))
                {
                    node = node.Next;
                    len++;
                }
                if (node == null || node == Snakes[ts].Coords.First)
                    return;

                bool removetail = true;
                Snakes[i].IncreaseLen += (Snakes[ts].Length - len) / 2;
                if ((Snakes[ts].Length * 1.0 / len) > 2)
                {
                    ProduceFruit(1, MapType.SlowFruit);
                    ProduceFruit(1, MapType.RushFruit);
                    ProduceFruit(1, MapType.HardFruit);
                }
                else
                    ProduceFruit(1, MapType.RushFruit);
                if (Snakes[i].IncreaseLen > 0)
                    removetail = false;

                ForceShorten(ts, len);
                Snakes[i].MoveStep();

                if (removetail == true)
                {
                    mapdata[taily, tailx] = MapType.Empty;
                    change.Add(new Coord(tailx, taily), MapType.Empty);
                }


                mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);

                if (removetail == false || (removetail == true && (tailx != headx || taily != heady)))
                {
                    mapdata[heady, headx] = MapType.SnakeStart + i;
                    change.Add(new Coord(headx, heady), MapType.SnakeStart + i);
                }

            }
            #endregion
            #region 撞到蛇头
            else if (NextStatus[i] < MapType.SnakeHeadStart + (byte)SnakeCount && NextStatus[i] >= MapType.SnakeHeadStart)
            {
                // 撞到了蛇头
                byte ts = NextStatus[i] - MapType.SnakeHeadStart;
                byte ti = i;
                if (Snakes[i].Length == Snakes[ts].Length)
                {

                }
                else if (Snakes[i].Length < Snakes[ts].Length)
                {
                    ti = ts;
                    ts = i;
                }

                ProduceFruit(2, MapType.SlowFruit);
                ProduceFruit(2, MapType.RushFruit);
                ProduceFruit(2, MapType.HardFruit);

                LinkedListNode<Coord> node = Snakes[ts].Coords.First, tnode = node.Next;
                int len = Snakes[ts].Length;

                ForceShorten(ts, 0);
                Snakes[ti].IncreaseLen += len;
                if (ti == i)
                {
                    Snakes[i].MoveStep();
                    mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                    change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);
                    mapdata[heady, headx] = MapType.SnakeStart + i;
                    change.Add(new Coord(headx, heady), MapType.SnakeStart + i);
                }
            }
            #endregion

            #region 道具
            else if (NextStatus[i] >= MapType.BlindFruit && NextStatus[i] < MapType.LastFruitPlaceHolder)
            {
                Snakes[i].MoveStep();

                mapdata[taily, tailx] = MapType.Empty;
                change.Add(new Coord(tailx, taily), MapType.Empty);

                mapdata[Snakes[i].Head.Y, Snakes[i].Head.X] = MapType.SnakeHeadStart + i;
                change.Add(Snakes[i].Head, MapType.SnakeHeadStart + i);
                if (taily != heady || tailx != headx)
                {
                    mapdata[heady, headx] = MapType.SnakeStart + i;
                    change.Add(new Coord(headx, heady), MapType.SnakeStart + i);
                }
                Dictionary<Coord, int> dic = null;
                switch (NextStatus[i])
                {
                    case MapType.BlindFruit:
                        dic = BlindFruits;
                        break;
                    case MapType.RushFruit:
                        dic = RushFruits;
                        break;
                    case MapType.SlowFruit:
                        dic = SlowFruits;
                        break;
                    case MapType.HardFruit:
                        dic = HardFruits;
                        break;
                }
                dic.Remove(Snakes[i].Head);
                if (Snakes[i].Inventory.ContainsKey(NextStatus[i]))
                    Snakes[i].Inventory[NextStatus[i]]++;
                else if (Snakes[i].Inventory.Count < 5)
                    Snakes[i].Inventory.Add(NextStatus[i], 1);
                else
                    ProduceFruit(1, NextStatus[i]);

            }
            #endregion

        }

        private List<Coord> ProduceFruit(int count, MapType fruitType)
        {
            if (!(fruitType < MapType.LastFruitPlaceHolder && fruitType >= MapType.Fruit))
                return null;
            Dictionary<Coord, int> dic = null;
            #region 选择存储数组
            switch (fruitType)
            {
                case MapType.Fruit:
                    dic = Fruits;
                    break;
                case MapType.HiddenSuperFruitAsShortenFruit:
                    dic = HiddenSuperFruits;
                    break;
                case MapType.HiddenSuperShortenFruitAsSuperFruit:
                    dic = HiddenSuperShortenFruits;
                    break;
                case MapType.ShortenFruit:
                    dic = ShortenFruits;
                    break;
                case MapType.SuperFruit:
                    dic = SuperFruits;
                    break;
                case MapType.SuperShortenFruit:
                    dic = SuperShortenFruits;
                    break;
                case MapType.Stone:
                    break;
                case MapType.BlindFruit:
                    dic = BlindFruits;
                    break;
                case MapType.RushFruit:
                    dic = RushFruits;
                    break;
                case MapType.SlowFruit:
                    dic = SlowFruits;
                    break;
                case MapType.HardFruit:
                    dic = HardFruits;
                    break;

                case MapType.PowerFruit:
                    break;
            }
            #endregion

            List<Coord> fruits = new List<Coord>();
            for (int i = 0; i < count; i++)
            {
                short x, y;
                Random rand = new Random();
                do
                {
                    x = (short)rand.Next(width);
                    y = (short)rand.Next(height);
                } while (mapdata[y, x] != MapType.Empty);
                mapdata[y, x] = fruitType;
                dic.Add(new Coord(x, y), rand.Next(5, int.MaxValue));
                fruits.Add(new Coord(x, y));
                change.Add(new Coord(x, y), fruitType);
            }
            return fruits;
        }

        public Dictionary<Coord, MapType> GetChanges()
        {
            var changecpy = change.Clone();
            change.Clear();
            return changecpy;
        }

        public void ForceShorten(int index, int len)
        {
            lock (locker)
            {
                if (index >= SnakeCount)
                    return;
                if (len == 0)
                {
                    RunSnakes[index] = false;
                }

                LinkedListNode<Coord> node = Snakes[index].Coords.First, tnode;
                while (len > 0)
                {
                    node = node.Next;
                    len--;
                }
                while (node != null)
                {
                    tnode = node.Next;
                    change.Add(new Coord(node.Value.X, node.Value.Y), MapType.Empty);
                    mapdata[node.Value.Y, node.Value.X] = MapType.Empty;
                    Snakes[index].Coords.Remove(node);
                    node = tnode;
                }
            }
        }

    }

    public class SpeedChangeData
    {
        public int Index;
        public int Inc;
        public Timer Timer;

        public SpeedChangeData(int index, int inc)
        {
            Index = index;
            Inc = inc;
        }

    }
    public class HardChangedArgs : EventArgs
    {
        private int index;
        private bool isSetHard;

        public int Index
        {
            get
            {
                return index;
            }
            private set
            {
                index = value;
            }
        }

        public bool IsSetHard
        {
            get
            {
                return isSetHard;
            }
            private set
            {
                isSetHard = value;
            }
        }

        public HardChangedArgs(int index, bool issethard)
        {
            this.index = index;
            this.isSetHard = issethard;
        }
    }
}
