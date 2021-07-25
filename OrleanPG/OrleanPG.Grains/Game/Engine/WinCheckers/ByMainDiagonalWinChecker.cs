using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game.Engine.WinCheckers
{
    public class ByMainDiagonalWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => map.IsMainDiagonalFilledBy(forPlayer.ToCellStatus()) ? new Win(0, GameAxis.MainDiagonal) : null;
    }
}
