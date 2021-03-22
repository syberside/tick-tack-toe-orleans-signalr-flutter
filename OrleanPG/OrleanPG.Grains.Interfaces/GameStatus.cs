namespace OrleanPG.Grains.Interfaces
{
    public record GameStatus(GameStatuses Status, bool?[,] GameMap);
}
