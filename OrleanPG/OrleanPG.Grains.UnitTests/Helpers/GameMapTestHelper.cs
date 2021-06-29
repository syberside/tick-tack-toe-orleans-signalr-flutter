using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.UnitTests.Helpers
{
    public static class GameMapTestHelper
    {
        public static (int, int) AdjustToGameSize(int x, int y) => (x % GameMap.GameSize, y % GameMap.GameSize);

    }
}
