using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game.Engine.WinCheckers
{
    public class BySideDiagonalWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => map.IsSideDiagonalFilledBy(forPlayer.ToCellStatus()) ? new Win(0, GameAxis.SideDiagonal) : null;
    }
}
