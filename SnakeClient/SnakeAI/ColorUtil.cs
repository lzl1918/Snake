using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SnakeAI
{
    public static class ColorUtil
    {
        public static Color GetRandomColor()
        {
            Random randomNum_1 = new Random(Guid.NewGuid().GetHashCode());
            System.Threading.Thread.Sleep(randomNum_1.Next(1));
            int int_Red = randomNum_1.Next(255);

            Random randomNum_2 = new Random((int)DateTime.Now.Ticks);
            int int_Green = randomNum_2.Next(255);

            Random randomNum_3 = new Random(Guid.NewGuid().GetHashCode());

            int int_Blue = randomNum_3.Next(255);
            int_Blue = (int_Red + int_Green > 380) ? int_Red + int_Green - 380 : int_Blue;
            int_Blue = (int_Blue > 255) ? 255 : int_Blue;


            return GetDarkerColor(Color.FromArgb(255, (byte)int_Red, (byte)int_Green, (byte)int_Blue));
        }

        //获取加深颜色
        public static Color GetDarkerColor(Color color)
        {
            const int max = 255;
            int increase = new Random(Guid.NewGuid().GetHashCode()).Next(50, 255); //还可以根据需要调整此处的值


            int r = Math.Abs(Math.Min(color.R - increase, max));
            int g = Math.Abs(Math.Min(color.G - increase, max));
            int b = Math.Abs(Math.Min(color.B - increase, max));


            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }
        //获取变浅颜色
        public static Color GetLighterColor(Color color)
        {
            const int max = 255;
            int increase = new Random(Guid.NewGuid().GetHashCode()).Next(50, 255); //还可以根据需要调整此处的值


            int r = Math.Abs(Math.Min(color.R + increase, max));
            int g = Math.Abs(Math.Min(color.G + increase, max));
            int b = Math.Abs(Math.Min(color.B + increase, max));


            return Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }

        public static Color GetDarker(this Color color)
        {
            return GetDarkerColor(color);
        }
        public static Color GetLighter(this Color color)
        {
            return GetLighterColor(color);
        }

    }
}
