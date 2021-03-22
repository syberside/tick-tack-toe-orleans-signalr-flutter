using System;
using System.Linq;
using System.Text;

namespace OrleanPG.Grains.UnitTests.Helpers
{
    public static class GameMapHelper
    {
        public static string ToMapString(this bool?[,] map)
        {
            if (map == null)
            {
                return "NULL";
            }
            var sb = new StringBuilder();
            for (var i = 0; i < map.GetLength(0); i++)
            {
                var row = Enumerable.Range(0, map.GetLength(0))
                 .Select(x => map[i, x])
                 .ToArray();
                sb.AppendLine($"{{{string.Join(" , ", row.Select(x => x == null ? "null" : x.ToString().ToLower()))}}}");
            }
            return sb.ToString();
        }
    }
}
