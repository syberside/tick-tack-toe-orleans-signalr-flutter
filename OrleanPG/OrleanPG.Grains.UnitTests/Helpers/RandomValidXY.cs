using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.UnitTests
{
    public record RandomValidXY
    {
        public int X { get; }
        public int Y { get; }

        public RandomValidXY(int pseudoX, int pseudoY)
        {
            X = pseudoX % GameMap.GameSize;
            Y = pseudoY % GameMap.GameSize;
        }
    }
}
