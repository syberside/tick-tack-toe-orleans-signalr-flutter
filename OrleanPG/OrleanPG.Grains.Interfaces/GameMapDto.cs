using OrleanPG.Grains.Interfaces.Infrastructure;
using System.Linq;

namespace OrleanPG.Grains.Interfaces
{
    public class GameMapDto
    {
        public CellStatus[,] Data { get; init; }

        public GameMapDto(CellStatus[,] data)
        {
            Data = data;
        }

        public GameMapDto() : this(new CellStatus[GameMapPoint.GameSize, GameMapPoint.GameSize]) { }

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
            return obj is GameMapDto map && Data.SequenceEquals(map.Data);
        }
    }
}
