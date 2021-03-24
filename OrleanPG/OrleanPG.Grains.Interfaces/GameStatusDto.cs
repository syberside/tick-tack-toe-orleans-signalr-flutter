namespace OrleanPG.Grains.Interfaces
{
    public record GameStatusDto(GameState Status, GameMap GameMap)
    {
        public GameStatusDto() : this(GameState.XTurn, new()) { }
    }
}
