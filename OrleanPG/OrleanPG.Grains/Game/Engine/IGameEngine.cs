using OrleanPG.Grains.Game.Engine.Actions;

namespace OrleanPG.Grains.Game.Engine
{
    public interface IGameEngine
    {
        GameEngineState Process<TAction>(TAction action, GameEngineState state) where TAction : IGameAction;
    }
}