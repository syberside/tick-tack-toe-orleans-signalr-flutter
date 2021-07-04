using OrleanPG.Grains.Game.Engine.Actions;

namespace OrleanPG.Grains.Game.Engine.GameEngineStates
{
    internal interface IGameEngineState
    {
        GameState Process(UserTurnAction action);
        GameState Process(TimeOutAction action);
    }
}
