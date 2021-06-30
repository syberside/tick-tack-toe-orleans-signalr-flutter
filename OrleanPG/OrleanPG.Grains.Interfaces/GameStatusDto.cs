namespace OrleanPG.Grains.Interfaces
{
    public record GameStatusDto(GameStatus Status, GameMapDto GameMap, string? PlayerXName, string? PlayerOName)
    {
        public GameStatusDto() : this(GameStatus.XTurn, new(), null, null) { }
    }
}
