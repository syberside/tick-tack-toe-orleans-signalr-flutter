using OrleanPG.Grains.Game.Engine.Actions;

namespace OrleanPG.Grains.Game.Engine
{
    public interface IGameEngine
    {
        GameState Process<TAction>(TAction action, GameState state) where TAction : IGameAction;
    }
}