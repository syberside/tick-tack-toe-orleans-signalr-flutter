using System.Linq;
using System.Text;

namespace OrleanPG.Grains.Interfaces
{
    public class GameMap
    {
        public const int GameSize = 3;

        public CellStatus[,] Data { get; init; }

        public GameMap(CellStatus[,] data)
        {
            Data = data;
        }

        public GameMap() : this(new CellStatus[GameSize, GameSize]) { }

        public GameMap Clone() => new((CellStatus[,])Data.Clone());

        public CellStatus this[int x, int y]
        {
            get => Data[x, y];
            set => Data[x, y] = value;
        }

        public string ToMapString(string separator = " | ", string emptyValue = " ", string xValue = "X", string oValue = "O")
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Data.GetLength(0); i++)
            {
                var row = Enumerable.Range(0, Data.GetLength(0))
                 .Select(x => Data[i, x])
                 .ToArray();
                sb.AppendLine($"{{{string.Join(separator, row.Select(x => x == CellStatus.Empty ? emptyValue : x == CellStatus.X ? xValue : oValue))}}}");
            }
            return sb.ToString();
        }


        public override int GetHashCode() => ToString().GetHashCode();

        public override string ToString() => ToMapString();

        public override bool Equals(object? obj)
        {
            return obj is GameMap map && map.ToString() == ToString();
        }
    }

    public enum CellStatus
    {
        Empty,
        X,
        O,
    }
}
