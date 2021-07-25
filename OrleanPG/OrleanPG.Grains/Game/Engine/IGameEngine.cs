using OrleanPG.Grains.Game.Engine.Actions;

namespace OrleanPG.Grains.Game.Engine
{
    public interface IGameEngine
    {
        GameState Process(IGameAction action, GameState state);
    }
}