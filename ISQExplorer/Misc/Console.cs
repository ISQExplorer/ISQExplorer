using System;

namespace ISQExplorer.Misc
{
    public static class Print
    {
        public static void Line(object s, ConsoleColor? color = null)
        {
            var colorBefore = Console.ForegroundColor;
            
            var col = color ?? Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.WriteLine(s.ToString());
            Console.ForegroundColor = colorBefore;
        }
    }
}