using FluentAssertions;
using OrleanPG.Grains.Interfaces;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Helpers
{
    public class GameMapHelperUnitTests
    {
        [Fact]
        public void ToMapString_Always_ReturnsCorrectString()
        {
            var map = new GameMap(new CellStatus[,]
            {
                {CellStatus.Empty,  CellStatus.X, CellStatus.O, },
                {CellStatus.Empty,  CellStatus.X, CellStatus.O, },
                {CellStatus.Empty,  CellStatus.X, CellStatus.O, },
            });
            map.ToMapString().Should().Be("{  | X | O}\r\n{  | X | O}\r\n{  | X | O}\r\n");
        }
    }
}
