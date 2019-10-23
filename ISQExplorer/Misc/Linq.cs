using System.Collections;
using System.Collections.Generic;
using System.IO;
using Npgsql.TypeHandlers.GeometricHandlers;

namespace ISQExplorer.Misc
{
    public static class Linq
    {
        public static IEnumerable<string> Lines(this StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}