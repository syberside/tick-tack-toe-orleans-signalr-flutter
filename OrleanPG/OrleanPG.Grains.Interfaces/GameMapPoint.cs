using System;

namespace OrleanPG.Grains.Interfaces
{
    public record GameMapPoint
    {
        public int X { get; }

        public int Y { get; }
        public static GameMapPoint Origin { get; } = new GameMapPoint(0, 0);

        public GameMapPoint(int x, int y)
        {
            ThrowIfOutOfGameMap(x, "X");
            ThrowIfOutOfGameMap(y, "Y");
            X = x;
            Y = y;
        }

        private static void ThrowIfOutOfGameMap(int x, string prefix)
        {
            if (x < 0 || x > GameMap.MaxIndex)
            {
                throw new ArgumentOutOfRangeException($"{prefix} should be positive and not greater than {GameMap.MaxIndex}. Received: {x}");
            }
        }
    }
}
