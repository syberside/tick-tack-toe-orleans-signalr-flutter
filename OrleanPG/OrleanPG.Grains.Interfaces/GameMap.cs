using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrleanPG.Grains.Interfaces
{
    public class GameMap
    {
        public const int GameSize = 3;

        public CellStatus[,] Data { get; init; }

        public bool HaveEmptyCells
        {
            get
            {
                for (var i = 0; i < GameSize; i++)
                {
                    for (var j = 0; j < GameSize; j++)
                    {
                        if (Data[i, j] == CellStatus.Empty)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

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

        public (int x, int y)[] GetAvailableCells() => GetAvailableCellsGenerator().ToArray();

        private IEnumerable<(int x, int y)> GetAvailableCellsGenerator()
        {
            for (var x = 0; x < GameSize; x++)
            {
                for (var y = 0; y < GameSize; y++)
                {
                    if (this[x, y] == CellStatus.Empty)
                    {
                        yield return (x, y);
                    }
                }
            }
        }
    }
}
