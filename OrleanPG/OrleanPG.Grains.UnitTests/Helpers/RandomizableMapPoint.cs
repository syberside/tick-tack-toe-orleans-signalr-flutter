using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.UnitTests
{
    public record RandomizableMapPoint : GameMapPoint
    {
        public RandomizableMapPoint(int pseudoX, int pseudoY)
            : base(pseudoX % GameMapPoint.GameSize, pseudoY % GameMapPoint.GameSize)
        {
        }
    }
}
