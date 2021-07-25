using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Interfaces;
using Orleans.Streams;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Game.GrainLogic
{
    public class GameGrainLogic
    {
        private readonly IGameStateStore _gameStateStore;
        private readonly IGameEngine _gameEngine;
        private readonly IGameLobby _gameLobby;
        private readonly IAsyncStream<GameStatusDto> _updateStream;
        private readonly IReminderHandle _reminderHandle;


        public GameGrainLogic(IGameStateStore gameStateStore,
                              IGameEngine gameEngine,
                              IGameLobby gameLobby,
                              IAsyncStream<GameStatusDto> updateStream,
                              IReminderHandle reminderHandle)
        {
            _gameStateStore = gameStateStore;
            _gameEngine = gameEngine;
            _gameLobby = gameLobby;
            _updateStream = updateStream;
            _reminderHandle = reminderHandle;
        }

        public async Task ProcessAction(IGameAction action)
        {
            var state = GetState();
            var newState = _gameEngine.Process(action, state);

            var updates = state != newState;
            if (updates)
            {
                await _gameStateStore.WriteState(newState);
                var update = await GetStatus(newState);
                await _updateStream.OnNextAsync(update);
            }

            await UpdateTimeoutReminder(newState);
        }

        private async Task UpdateTimeoutReminder(GameState newState)
        {
            if (newState.Status.IsEndStatus())
            {
                await _reminderHandle.Reset();
            }
            else
            {
                await _reminderHandle.Set();
            }
        }

        private async Task<GameStatusDto> GetStatus(GameState state)
        {
            var userNames = await _gameLobby.ResolveUserNamesAsync(state.XPlayer, state.OPlayer);
            var gameMapDto = new GameMapDto(state.Map.DataSnapshot());
            var result = new GameStatusDto(state.Status, gameMapDto, userNames[0], userNames[1]);
            return result;
        }

        public Task<GameStatusDto> GetStatus() => GetStatus(GetState());

        public GameState GetState() => _gameStateStore.GetState();
    }
}

