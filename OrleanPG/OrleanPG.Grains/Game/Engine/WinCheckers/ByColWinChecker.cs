using OrleanPG.Grains.Interfaces;
using System.Linq;

namespace OrleanPG.Grains.Game.Engine.WinCheckers
{
    public class ByColWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => Enumerable.Range(0, GameMap.GameSize)
            .Where(y => map.IsColFilledBy(y, forPlayer.ToCellStatus()))
            .Select(y => new Win(y, GameAxis.Y)).FirstOrDefault();
    }
}
