using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game.Engine.WinCheckers
{
    public interface IWinChecker
    {
        Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer);
    }
}
