namespace OrleanPG.Grains.Interfaces
{
    public record GameStatusDto(GameState Status, GameMap GameMap, string? PlayerXName, string? PlayerOName)
    {
        public GameStatusDto() : this(GameState.XTurn, new(), null, null) { }
    }
}
