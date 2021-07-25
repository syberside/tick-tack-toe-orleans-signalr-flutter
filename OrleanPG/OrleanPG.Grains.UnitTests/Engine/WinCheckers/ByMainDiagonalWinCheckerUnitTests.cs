using FluentAssertions;
using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.WinCheckers;
using OrleanPG.Grains.Interfaces;
using Xunit;

namespace OrleanPG.Grains.UnitTests.Engine.WinCheckers
{
    public class ByMainDiagonalWinCheckerUnitTests
    {
        private readonly ByMainDiagonalWinChecker _checker;

        public ByMainDiagonalWinCheckerUnitTests()
        {
            _checker = new ByMainDiagonalWinChecker();
        }

        [Fact]
        public void CheckIfWin_OnWinByMainDiagonal_ReturnsWinResult()
        {
            var gameMap = new GameMap(new CellStatus[,]
            {
                {CellStatus.X,  CellStatus.X, CellStatus.X, },
                {CellStatus.X, CellStatus.X, CellStatus.X, },
                {CellStatus.O,  CellStatus.X, CellStatus.X, },
            });

            var result = _checker.CheckIfWin(gameMap, PlayerParticipation.X);

            result.Should().BeEquivalentTo(new Win(0, GameAxis.MainDiagonal));
        }

        [Fact]
        public void CheckIfWin_OnNoWinByMainDoagonal_ReturnsNull()
        {
            var gameMap = new GameMap(new CellStatus[,]
            {
                {CellStatus.O,  CellStatus.X, CellStatus.X, },
                {CellStatus.X, CellStatus.X, CellStatus.X, },
                {CellStatus.X,  CellStatus.X, CellStatus.X, },
            });

            var result = _checker.CheckIfWin(gameMap, PlayerParticipation.X);

            result.Should().BeNull();
        }
    }
}
