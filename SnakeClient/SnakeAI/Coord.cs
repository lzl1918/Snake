using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAI
{
    [Serializable]
    public class Coord : ISerializable
    {
        short x, y;

        public readonly static Coord Left = new Coord(-1, 0);
        public readonly static Coord Up = new Coord(0, -1);
        public readonly static Coord Down = new Coord(0, 1);
        public readonly static Coord Right = new Coord(1, 0);


        public short X
        {
            get
            {
                return x;
            }

            set
            {
                x = value;
            }
        }

        public short Y
        {
            get
            {
                return y;
            }

            set
            {
                y = value;
            }
        }

        public double Distance
        {
            get
            {
                return Math.Sqrt(x * x + y * y);
            }
        }

        public Coord(short x = 0, short y = 0)
        {
            X = x;
            Y = y;
        }

        public Coord(SerializationInfo info, StreamingContext c)
        {
            x = info.GetInt16("x");
            y = info.GetInt16("y");
        }

        //public override string ToString()
        //{
        //    return string.Format("({0}, {1})", x, y);
        //}

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() == this.GetType())
                return this.x == (obj as Coord).x && this.y == (obj as Coord).y;
            else
                return false;
        }
        public static bool operator ==(Coord left, Coord right)
        {
            bool ln = object.Equals(left, null), rn = object.Equals(right, null);
            if (ln == true && rn == true)
                return true;
            else if (ln == false && rn == true)
                return false;
            else if (ln == true && rn == false)
                return false;
            else
                return (left.x == right.x && left.y == right.y);
        }
        public static bool operator !=(Coord left, Coord right)
        {
            bool ln = object.Equals(left, null), rn = object.Equals(right, null);
            if (ln == true && rn == true)
                return false;
            else if (ln == false && rn == true)
                return true;
            else if (ln == true && rn == false)
                return true;
            else
                return !(left.x == right.x && left.y == right.y);
        }

        public static Coord operator +(Coord left, Coord right)
        {
            return new Coord((short)(left.x + right.x), (short)(left.y + right.y));
        }
        public static Coord operator -(Coord left, Coord right)
        {
            return new Coord((short)(left.x - right.x), (short)(left.y - right.y));
        }

        public static int operator *(Coord left, Coord right)
        {
            return left.x * right.x + left.y * right.y;
        }


        public Coord Normalize()
        {
            double len = Distance;
            if (len != 0)
                return new Coord((short)(x / len), (short)(y / len));
            else
                return new Coord(0, 0);
        }

        public bool IsParallel(Coord cod)
        {
            return x * cod.y == y * cod.x;
        }
        public bool IsReverseDirection(Coord cod)
        {
            return IsParallel(cod) && this * cod < 0;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("x", this.x);
            info.AddValue("y", this.y);
        }
    }
}
