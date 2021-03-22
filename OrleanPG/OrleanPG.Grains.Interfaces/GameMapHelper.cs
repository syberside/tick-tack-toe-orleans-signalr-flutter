using System;
using System.Linq;
using System.Text;

namespace OrleanPG.Grains.Interfaces
{
    public static class GameMapHelper
    {
        public static string ToMapString(this bool?[,] map, string separator = " , ", string nullValue = "null", string xValue = "true", string oValue = "false")
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
                sb.AppendLine($"{{{string.Join(separator, row.Select(x => x == null ? nullValue : x.Value ? xValue : oValue))}}}");
            }
            return sb.ToString();
        }
    }
}
