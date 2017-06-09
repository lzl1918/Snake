using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console
{
    class Program
    {
        static void Main(string[] args)
        {
            while(true)
            {
                string op = System.Console.ReadLine();
                if(op == "1")
                {
                    string initval = System.Console.ReadLine();
                    int intval = int.Parse(initval);
                    Extern.Init(20, 20, 1, 1, 5, 8);
                }
                else
                {
                    string initval = System.Console.ReadLine();
                    int intval = int.Parse(initval);
                    int outval = Extern.Move(1, 1, 5, 8);
                    System.Console.WriteLine(outval);
                }
            }
        }
    }
}
