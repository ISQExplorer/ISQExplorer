using System;

namespace ISQExplorer.Misc
{
    public static class Print
    {
        /// <summary>
        /// Prints a line of text to stdout with an optional color.
        /// </summary>
        /// <param name="s">The object to print. Its toString() representation will be used.</param>
        /// <param name="color">The color to print the line in. By default this uses the default color of the terminal.</param>
        public static void Line(object? s, ConsoleColor? color = null)
        {
            var colorBefore = Console.ForegroundColor;

            var col = color ?? Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.WriteLine(s?.ToString());
            Console.ForegroundColor = colorBefore;
        }

        /// <summary>
        /// Prints a line of text to stderr with an optional color.
        /// </summary>
        /// <param name="s">The object to print. Its toString() representation will be used.</param>
        /// <param name="color">The color to print the line in. By default this uses the default color of the terminal.</param>
        public static void Error(object s, ConsoleColor? color = null)
        {
            var colorBefore = Console.ForegroundColor;

            var col = color ?? Console.ForegroundColor;
            Console.ForegroundColor = col;
            Console.Error.WriteLine(s.ToString());
            Console.ForegroundColor = colorBefore;
        }
    }
}