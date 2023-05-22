using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MgcPrxyDrftr.lib
{
    internal static class Helpers
    {
        public static bool Write(string text, int posX, int posY)
        {
            var (left, top) = Console.GetCursorPosition();
            Console.SetCursorPosition(posX, posY);
            Console.Write(text);
            Console.SetCursorPosition(left, top);

            return true;
        }
    }
}
