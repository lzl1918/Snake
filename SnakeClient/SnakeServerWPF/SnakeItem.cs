﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lib;

namespace SnakeServerWPF
{
    public class SnakeItem
    {
        LinkedList<Coord> coords = null;
        Coord direction = new Coord(0, 0);
        int increaseLen = 0;
        Dictionary<MapType, byte> inventory = new Dictionary<MapType, byte>();

        public int Length
        {
            get
            {
                return coords.Count;
            }
        }
        public Coord Head
        {
            get { return coords.First.Value; }
        }
        public Coord Tail
        {
            get { return coords.Last.Value; }
        }

        public LinkedList<Coord> Coords
        {
            get
            {
                return coords;
            }
            private set
            {
                coords = value;
            }
        }

        public Coord Direction
        {
            get
            {
                return direction;
            }

            set
            {
                if (coords.Count > 1 && direction.IsReverseDirection(value))
                    return;
                if (coords.Count > 1 && value.IsReverseDirection(coords.First.Next.Value - coords.First.Value))
                    return;
                else
                    direction = value;
            }
        }

        public int IncreaseLen
        {
            get
            {
                return increaseLen;
            }
            set
            {
                increaseLen = value;
            }
        }

        public Dictionary<MapType, byte> Inventory
        {
            get
            {
                return inventory;
            }
            set
            {
                inventory = value;
            }
        }

        public SnakeItem(Coord defaultPosition, Coord defaultDirection)
        {
            coords = new LinkedList<Coord>();
            coords.AddLast(defaultPosition);
            Direction = defaultDirection;
        }

        public void MoveStep()
        {
            short tx = (short)(coords.First.Value.X + direction.X);
            short ty = (short)(coords.First.Value.Y + direction.Y);
            if (IncreaseLen > 0)
            {
                coords.AddFirst(new Coord(tx, ty));
                IncreaseLen--;
            }
            else
            {
                coords.RemoveLast();
                coords.AddFirst(new Coord(tx, ty));
            }
        }
    }
}
