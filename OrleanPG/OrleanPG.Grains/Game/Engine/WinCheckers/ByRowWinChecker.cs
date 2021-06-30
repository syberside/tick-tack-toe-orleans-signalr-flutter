using OrleanPG.Grains.Interfaces;
using System.Linq;

namespace OrleanPG.Grains.Game.Engine.WinCheckers
{
    public class ByRowWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => Enumerable.Range(0, GameMapPoint.GameSize)
            .Where(x => map.IsRowFilledBy(x, forPlayer.ToCellStatus()))
            .Select(x => new Win(x, GameAxis.X)).FirstOrDefault();
    }
}
