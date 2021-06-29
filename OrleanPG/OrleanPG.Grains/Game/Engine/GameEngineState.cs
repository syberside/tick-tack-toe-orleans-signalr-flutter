using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Grains.Game.Engine
{
    public record GameEngineState(GameMap Map, GameState GameState);
}
