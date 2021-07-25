using FluentAssertions;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Engine.WinCheckers
{
    public class ByRowWinCheckerUnitTests
    {
        private readonly ByRowWinChecker _checker;

        public ByRowWinCheckerUnitTests()
        {
            _checker = new ByRowWinChecker();
        }

        [Theory]
        [InlineData(PlayerParticipation.X, 1)]
        [InlineData(PlayerParticipation.O, 2)]
        public void CheckIfWin_OnWinByRow_ReturnsWinResult(PlayerParticipation participation, int expectedIndex)
        {
            var gameMap = new GameMap(new CellStatus[,]
            {
                {CellStatus.Empty,  CellStatus.Empty, CellStatus.Empty, },
                {CellStatus.X, CellStatus.X, CellStatus.X, },
                {CellStatus.O,  CellStatus.O, CellStatus.O, },
            });

            var result = _checker.CheckIfWin(gameMap, participation);

            result.Should().BeEquivalentTo(new Win(expectedIndex, GameAxis.X));
        }

        [Fact]
        public void CheckIfWin_OnNoWinByRow_ReturnsNull()
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
