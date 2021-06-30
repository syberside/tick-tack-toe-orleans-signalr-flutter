using OrleanPG.Grains.Interfaces;
using OrleanPG.Grains.Interfaces.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrleanPG.Grains.Game.Engine
{
    public class GameMap
    {
        private CellStatus[,] Data { get; }

        public GameMap(CellStatus[,] arrayToClone)
        {
            Data = (CellStatus[,])arrayToClone.Clone();
        }

        public bool HaveEmptyCells => EnumerateAvailableCells().Any();

        public CellStatus[,] DataSnapshot() => (CellStatus[,])Data.Clone();


        public GameMap Update(GameMapPoint position, CellStatus status)
        {
            var result = new GameMap(Data);
            result[position.X, position.Y] = status;
            return result;
        }

        public static GameMap FilledWith(CellStatus cellStatus) => new(new CellStatus[,]
        {
            { cellStatus, cellStatus, cellStatus, },
            { cellStatus, cellStatus, cellStatus, },
            { cellStatus, cellStatus, cellStatus, },
        });

        public GameMap() : this(new CellStatus[GameMapPoint.GameSize, GameMapPoint.GameSize]) { }

        private CellStatus this[int x, int y]
        {
            get => Data[x, y];
            set => Data[x, y] = value;
        }

        public CellStatus this[GameMapPoint position]
        {
            get => Data[position.X, position.Y];
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

        public override bool Equals(object? obj)
        {
            return obj is GameMap map && Data.SequenceEquals(map.Data);
        }

        public GameMapPoint[] GetAvailableCells() => EnumerateAvailableCells().ToArray();

        private IEnumerable<GameMapPoint> EnumerateAvailableCells()
        {
            for (var x = 0; x < GameMapPoint.GameSize; x++)
            {
                for (var y = 0; y < GameMapPoint.GameSize; y++)
                {
                    if (this[x, y] == CellStatus.Empty)
                    {
                        yield return new GameMapPoint(x, y);
                    }
                }
            }
        }

        public bool IsColFilledBy(int y, CellStatus stepBy)
        {
            for (var i = 0; i < GameMapPoint.GameSize; i++)
            {
                if (this[i, y] != stepBy)
                {
                    return false;
                }
                if (i == GameMapPoint.MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsRowFilledBy(int x, CellStatus stepBy)
        {
            for (var i = 0; i < GameMapPoint.GameSize; i++)
            {
                if (this[x, i] != stepBy)
                {
                    return false;
                }
                if (i == GameMapPoint.MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsMainDiagonalFilledBy(CellStatus stepBy)
        {
            for (var i = 0; i < GameMapPoint.GameSize; i++)
            {
                if (this[i, i] != stepBy)
                {
                    return false;
                }
                if (i == GameMapPoint.MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsSideDiagonalFilledBy(CellStatus stepBy)
        {
            for (var i = 0; i < GameMapPoint.GameSize; i++)
            {
                if (this[GameMapPoint.MaxIndex - i, i] != stepBy)
                {
                    return false;
                }
                if (i == GameMapPoint.MaxIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsCellBusy(GameMapPoint point) => this[point.X, point.Y] != CellStatus.Empty;
    }
}
