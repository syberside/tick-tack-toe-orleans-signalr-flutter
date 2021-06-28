using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrleanPG.Grains.Interfaces
{
    public class GameMap
    {
        public const int GameSize = 3;
        public const int MaxIndex = GameSize - 1;

        public CellStatus[,] Data { get; init; }

        public bool HaveEmptyCells => EnumerateAvailableCells().Any();
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


        public override int GetHashCode()
        {
            var hash = 17;
            var array = Data.Cast<int>();
            foreach (var arrayItem in array)
            {
                hash = hash * 31 + arrayItem;
            }
            return hash;
        }

        public override string ToString() => ToMapString();

        public override bool Equals(object? obj)
        {
            return obj is GameMap map && SequenceEquals(Data, map.Data);
        }

        public (int x, int y)[] GetAvailableCells() => EnumerateAvailableCells().ToArray();

        private IEnumerable<(int x, int y)> EnumerateAvailableCells()
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

        private static bool SequenceEquals<T>(T[,] a, T[,] b) => a.Rank == b.Rank
           && Enumerable.Range(0, a.Rank).All(d => a.GetLength(d) == b.GetLength(d))
           && a.Cast<T>().SequenceEqual(b.Cast<T>());

        public bool IsRowFilledBy(int y, CellStatus stepBy)
        {
            for (var i = 0; i < GameSize; i++)
            {
                if (this[i, y] != stepBy)
                {
                    return false;
                }
                if (i == MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsColFilledBy(int x, CellStatus stepBy)
        {
            for (var i = 0; i < GameSize; i++)
            {
                if (this[x, i] != stepBy)
                {
                    return false;
                }
                if (i == MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMainDiagonalFilledBy(CellStatus stepBy)
        {
            for (var i = 0; i < GameSize; i++)
            {
                if (this[i, i] != stepBy)
                {
                    return false;
                }
                if (i == MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSideDiagonalFilledBy(CellStatus stepBy)
        {
            for (var i = 0; i < GameSize; i++)
            {
                if (this[MaxIndex - i, i] != stepBy)
                {
                    return false;
                }
                if (i == MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
