using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
    public class Extern
    {
        [DllImport("lib.dll", EntryPoint = "move", CallingConvention = CallingConvention.Cdecl)]
        public extern static int Move(int snakeX, int snakeY, int foodX, int foodY);

        [DllImport("lib.dll", EntryPoint = "init", CallingConvention = CallingConvention.Cdecl)]
        public extern static void Init(int stageWidth, int stageHeight, int snakeX, int snakeY, int foodX, int foodY);
    }
}
