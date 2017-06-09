using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAI
{
    public class Map
    {
        public static Coord GetMaxium(int[,] distance, Coord source)
        {
            int maxdistance = 0;
            int width = distance.GetLength(1);
            int height = distance.GetLength(0);
            Coord crd = null;
            if (source.X + 1 >= 0 && source.X + 1 < width && distance[source.Y, source.X + 1] > maxdistance)
                crd = new Coord(1, 0);
            if (source.X - 1 >= 0 && source.X - 1 < width && distance[source.Y, source.X - 1] > maxdistance)
                crd = new Coord(-1, 0);
            if (source.Y + 1 >= 0 && source.Y + 1 < height && distance[source.Y + 1, source.X] > maxdistance)
                crd = new Coord(0, 1);
            if (source.Y - 1 >= 0 && source.Y - 1 < height && distance[source.Y - 1, source.X] > maxdistance)
                crd = new Coord(0, -1);
            return crd;
        }
        public static Coord GetMinum(int[,] distance, Coord source)
        {
            int mindistance = int.MaxValue;
            Coord crd = null;
            int width = distance.GetLength(1);
            int height = distance.GetLength(0);
            if (source.X + 1 >= 0 && source.X + 1 < width && distance[source.Y, source.X + 1] < mindistance && distance[source.Y, source.X + 1] != 0)
                crd = new Coord(1, 0);
            if (source.X - 1 >= 0 && source.X - 1 < width && distance[source.Y, source.X - 1] < mindistance && distance[source.Y, source.X - 1] != 0)
                crd = new Coord(-1, 0);
            if (source.Y + 1 >= 0 && source.Y + 1 < height && distance[source.Y + 1, source.X] < mindistance && distance[source.Y + 1, source.X] != 0)
                crd = new Coord(0, 1);
            if (source.Y - 1 >= 0 && source.Y - 1 < height && distance[source.Y - 1, source.X] < mindistance && distance[source.Y - 1, source.X] != 0)
                crd = new Coord(0, -1);
            return crd;
        }


        public SnakeItem Snake = null;

        public Coord Fruit = null;

        int width, height;

        MapType[,] mapdata = null;

        public MapType NextStep = MapType.Empty;

        public Dictionary<Coord, MapType> Changes = new Dictionary<Coord, MapType>();

        Random rand = new Random();
        public MapType[,] MapData
        {
            get
            {
                return mapdata;
            }
        }

        public MapType this[Coord crd]
        {
            get
            {
                if (crd.X < 0 || crd.X >= width || crd.Y < 0 || crd.Y >= height)
                    return MapType.Unknow;
                else
                    return mapdata[crd.Y, crd.X];
            }
            set { mapdata[crd.Y, crd.X] = value; }
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

        private Map()
        {

        }
        public Map(int width, int height)
        {
            this.width = width;
            this.height = height;
            mapdata = new MapType[height, width];
            Snake = new SnakeItem(new Coord(0, 0), new Coord(1, 0));
            mapdata[Snake.Head.Y, Snake.Head.X] = MapType.Snake;
            Changes.Add(new Coord(Snake.Head.X, Snake.Head.Y), MapType.Snake);
            ProduceFruit();
        }

        private void EnumNextMoveStatus()
        {
            int tx, ty;

            tx = Snake.Coords.First.Value.X + Snake.Direction.X;
            ty = Snake.Coords.First.Value.Y + Snake.Direction.Y;
            if (!(tx >= 0 && tx < width && ty >= 0 && ty < height))
                NextStep = MapType.Unknow;
            else
                NextStep = mapdata[ty, tx];
        }

        public void MoveNext()
        {
            short tailx, taily, headx, heady, nextx, nexty;

            EnumNextMoveStatus();
            tailx = Snake.Tail.X;
            taily = Snake.Tail.Y;
            headx = Snake.Head.X;
            heady = Snake.Head.Y;
            nextx = (short)(headx + Snake.Direction.X);
            nexty = (short)(heady + Snake.Direction.Y);


            #region 正常移动
            if (NextStep == MapType.Empty)
            {
                Snake.MoveStep();

                mapdata[taily, tailx] = MapType.Empty;
                Changes.Add(new Coord(tailx, taily), MapType.Empty);

                mapdata[Snake.Head.Y, Snake.Head.X] = MapType.Snake;
                Changes.Add(new Coord(Snake.Head.X, Snake.Head.Y), MapType.Snake);
            }
            #endregion
            #region 水果
            else if (NextStep == MapType.Fruit)
            {
                Snake.IncreaseLen++;
                Snake.MoveStep();

                mapdata[Snake.Head.Y, Snake.Head.X] = MapType.Snake;
                Changes.Add(new Coord(Snake.Head.X, Snake.Head.Y), MapType.Snake);

                ProduceFruit();
            }
            #endregion
            #region 撞到自己
            else if (NextStep == MapType.Snake)
            {

            }
            #endregion

        }

        private void ProduceFruit()
        {
            short x, y;
            Random rand = new Random();
            do
            {
                x = (short)rand.Next(width);
                y = (short)rand.Next(height);
            } while (mapdata[y, x] != MapType.Empty);
            mapdata[y, x] = MapType.Fruit;
            Fruit = new Coord(x, y);
            Changes.Add(new Coord(x, y), MapType.Fruit);
        }

        //public bool EnumReachTail()
        //{
        //    bool find = false;
        //    while (true)
        //    {
        //        int[,] dist = BFS(Snake.Tail, out find);
        //        if (find == true)
        //        {
        //            Snake.Direction = GetMaxium(dist, Snake.Head);
        //            MoveNext();
        //        }
        //        else
        //            break;
        //    }
        //}

        public List<Coord> BFS( Coord target)
        {
            if (Snake.Head == target)
            {
                return new List<Coord>() { new Coord(target.X, target.Y) };
            }

            List<Coord> path = new List<Coord>();
            Queue<Coord> que = new Queue<Coord>();
            Coord[,] parent = new Coord[Height, Width];
            Coord crd = null;
            bool find = false;
            que.Enqueue(Snake.Head);
            parent[Snake.Head.Y, Snake.Head.X] = Snake.Head;
            #region BFS
            while (que.Count > 0)
            {
                crd = que.Dequeue();
                if (crd.X + 1 < Width && crd.X + 1 >= 0 && parent[crd.Y, crd.X + 1] == null)
                {

                    if (crd.Y == target.Y && crd.X + 1 == target.X)
                    {
                        parent[crd.Y, crd.X + 1] = crd;
                        crd = new Coord((short)(crd.X + 1), crd.Y);
                        find = true;
                        break;
                    }
                    else if (MapData[crd.Y, crd.X + 1] == MapType.Empty)
                    {
                        que.Enqueue(new Coord((short)(crd.X + 1), crd.Y));
                        parent[crd.Y, crd.X + 1] = crd;
                    }
                }
                if (crd.X - 1 < Width && crd.X - 1 >= 0 && parent[crd.Y, crd.X - 1] == null)
                {
                    if (crd.Y == target.Y && crd.X - 1 == target.X)
                    {
                        parent[crd.Y, crd.X - 1] = crd;
                        crd = new Coord((short)(crd.X - 1), crd.Y);
                        find = true;
                        break;
                    }
                    else if (MapData[crd.Y, crd.X - 1] == MapType.Empty)
                    {
                        que.Enqueue(new Coord((short)(crd.X - 1), crd.Y));
                        parent[crd.Y, crd.X - 1] = crd;
                    }
                }
                if (crd.Y + 1 < Height && crd.Y + 1 >= 0 && parent[crd.Y + 1, crd.X] == null)
                {
                    if (crd.Y + 1 == target.Y && crd.X == target.X)
                    {
                        parent[crd.Y + 1, crd.X] = crd;
                        crd = new Coord(crd.X, (short)(crd.Y + 1));
                        find = true;
                        break;
                    }
                    else if (MapData[crd.Y + 1, crd.X] == MapType.Empty)
                    {
                        que.Enqueue(new Coord(crd.X, (short)(crd.Y + 1)));
                        parent[crd.Y + 1, crd.X] = crd;
                    }
                }
                if (crd.Y - 1 < Height && crd.Y - 1 >= 0 && parent[crd.Y - 1, crd.X] == null)
                {
                    if (crd.Y - 1 == target.Y && crd.X == target.X)
                    {
                        parent[crd.Y - 1, crd.X] = crd;
                        crd = new Coord(crd.X, (short)(crd.Y - 1));
                        find = true;
                        break;
                    }
                    else if (MapData[crd.Y - 1, crd.X] == MapType.Empty)
                    {
                        que.Enqueue(new Coord(crd.X, (short)(crd.Y - 1)));
                        parent[crd.Y - 1, crd.X] = crd;
                    }
                }
            }
            #endregion

            if (find == true)
            {
                while (parent[crd.Y, crd.X] != crd)
                {
                    path.Insert(0, crd);
                    crd = parent[crd.Y, crd.X];
                }
                path.Insert(0, crd);
                return path;
            }
            else
                return null;
        }
        public int[,] BFS(Coord target, out bool find)
        {
            if (Snake.Head == target)
            {
                find = true;
                return null;
            }

            Queue<Coord> que = new Queue<Coord>();
            int[,] distance = new int[Height, Width];
            Coord crd = null;
            que.Enqueue(Snake.Head);
            int layer = 1;
            distance[Snake.Head.Y, Snake.Head.X] = layer;
            que.Enqueue(null);
            int[,] directions = new int[4, 2] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
            bool allset = true;
            int hx = Snake.Head.X, hy = Snake.Head.Y;
            int i;
            #region BFS
            while (que.Count > 0)
            {
                crd = que.Dequeue();
                allset = true;
                for (i = 0; i < 4; i++)
                {
                    if (hy + directions[i, 1] >= 0 && hy + directions[i, 1] < height && hx + directions[i, 0] >= 0 && hx + directions[i, 0] < width && MapData[hy + directions[i, 1], hx + directions[i, 0]] == MapType.Empty && distance[hy + directions[i, 1], hx + directions[i, 0]] == 0)
                    {
                        allset = false;
                        break;
                    }
                }
                if (allset == true)
                    break;

                if (crd == null)
                {
                    layer += 1;
                    if (que.Count > 0)
                        que.Enqueue(null);
                }
                else
                {
                    if (crd.X + 1 < Width && crd.X + 1 >= 0 && distance[crd.Y, crd.X + 1] == 0 && MapData[crd.Y, crd.X + 1] == MapType.Empty)
                    {
                        que.Enqueue(new Coord((short)(crd.X + 1), crd.Y));
                        distance[crd.Y, crd.X + 1] = layer + 1;
                    }
                    if (crd.X - 1 < Width && crd.X - 1 >= 0 && distance[crd.Y, crd.X - 1] == 0 && MapData[crd.Y, crd.X - 1] == MapType.Empty)
                    {
                        que.Enqueue(new Coord((short)(crd.X - 1), crd.Y));
                        distance[crd.Y, crd.X - 1] = layer + 1;
                    }
                    if (crd.Y + 1 < Height && crd.Y + 1 >= 0 && distance[crd.Y + 1, crd.X] == 0 && MapData[crd.Y + 1, crd.X] == MapType.Empty)
                    {
                        que.Enqueue(new Coord(crd.X, (short)(crd.Y + 1)));
                        distance[crd.Y + 1, crd.X] = layer + 1;
                    }
                    if (crd.Y - 1 < Height && crd.Y - 1 >= 0 && distance[crd.Y - 1, crd.X] == 0 && MapData[crd.Y - 1, crd.X] == MapType.Empty)
                    {
                        que.Enqueue(new Coord(crd.X, (short)(crd.Y - 1)));
                        distance[crd.Y - 1, crd.X] = layer + 1;
                    }
                }

            }
            #endregion
            allset = false;
            for (i = 0; i < 4; i++)
            {
                if (hy + directions[i, 1] >= 0 && hy + directions[i, 1] < height && hx + directions[i, 0] >= 0 && hx + directions[i, 0] < width && MapData[hy + directions[i, 1], hx + directions[i, 0]] == MapType.Empty && distance[hy + directions[i, 1], hx + directions[i, 0]] != 0)
                {
                    allset = true;
                    break;
                }
            }
            find = allset;
            return distance;

        }

        public List<KeyValuePair<Coord, MapType>> GetChanges()
        {
            var changecpy = Changes.ToList();
            Changes.Clear();
            return changecpy;
        }

        public Map CopyMove(List<Coord> path)
        {
            Map map = new Map();
            map.width = width;
            map.height = height;
            map.Fruit = new Coord(Fruit.X, Fruit.Y);
            map.Snake = new SnakeItem(new Coord(Fruit.X, Fruit.Y), new Coord(0, 0));
            map.mapdata = new MapType[height, width];
            int remainLen = Snake.Length;
            int i = path.Count - 1;
            map.mapdata[path[i].Y, path[i].X] = MapType.Snake;
            i--;
            remainLen--;
            while (i >= 0 && remainLen > 0)
            {
                map.mapdata[path[i].Y, path[i].X] = MapType.Snake;
                map.Snake.Coords.AddLast(new Coord(path[i].X, path[i].Y));
                i--;
                remainLen--;
            }
            LinkedListNode<Coord> node = Snake.Coords.First;
            while (remainLen > 0)
            {
                map.mapdata[node.Value.Y, node.Value.X] = MapType.Snake;
                map.Snake.Coords.AddLast(new Coord(node.Value.X, node.Value.Y));
                remainLen--;
                node = node.Next;
            }
            return map;
        }
        
    }
}
