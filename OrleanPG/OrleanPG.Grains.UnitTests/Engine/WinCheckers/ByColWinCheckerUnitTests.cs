using FluentAssertions;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Engine.WinCheckers
{
    public class ByColWinCheckerUnitTests
    {
        private readonly ByColWinChecker _checker;

        public ByColWinCheckerUnitTests()
        {
            _checker = new ByColWinChecker();
        }

        [Theory]
        [InlineData(PlayerParticipation.X, 1)]
        [InlineData(PlayerParticipation.O, 2)]
        public void CheckIfWin_OnWinByCol_ReturnsWinResult(PlayerParticipation participation, int expectedIndex)
        {
            var gameMap = new GameMap(new CellStatus[,]
            {
                {CellStatus.Empty,  CellStatus.X, CellStatus.O, },
                {CellStatus.Empty, CellStatus.X, CellStatus.O, },
                {CellStatus.Empty,  CellStatus.X, CellStatus.O, },
            });

            var result = _checker.CheckIfWin(gameMap, participation);

            result.Should().BeEquivalentTo(new Win(expectedIndex, GameAxis.Y));
        }

        [Fact]
        public void CheckIfWin_OnNoWinByCol_ReturnsNull()
        {
            var gameMap = new GameMap(new CellStatus[,]
            {
                {CellStatus.X,  CellStatus.O, CellStatus.X, },
                {CellStatus.X, CellStatus.X, CellStatus.X, },
                {CellStatus.X,  CellStatus.X, CellStatus.X, },
            });

            var result = _checker.CheckIfWin(gameMap, PlayerParticipation.O);

            result.Should().BeNull();
        }
    }
}
