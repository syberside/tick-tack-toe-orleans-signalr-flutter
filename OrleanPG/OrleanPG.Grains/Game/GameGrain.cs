using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Game
{
    public class GameGrain : Grain, IGame, IGameInitializer
    {
        private readonly IPersistentState<GameState> _gameStateHolder;
        private readonly IGrainIdProvider _grainIdProvider;
        private readonly IGameEngine _gameEngine;

        public const string TimeoutCheckReminderName = "timeout_check";
        public static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMinutes(1);

        public GameGrain(
            [PersistentState("game_game_state", "game_state_store")]
            IPersistentState<GameState> gameState,
            IGrainIdProvider grainIdProvider,
            IGameEngine gameEngine)
        {
            _gameStateHolder = gameState;
            _grainIdProvider = grainIdProvider;
            _gameEngine = gameEngine;
        }


        public async Task<GameStatusDto> StartAsync(AuthorizationToken playerX, AuthorizationToken playerO)
            => await ProcessAction(new InitializeAction(playerX, playerO));

        public async Task<GameStatusDto> TurnAsync(GameMapPoint position, AuthorizationToken player)
            => await ProcessAction(new UserTurnAction(position, GetParticipation(GetState(), player)));
        private async Task<GameStatusDto> ProcessAction(IGameAction action)
        {
            var state = GetState();
            var newState = _gameEngine.Process(action, state);

            GameStatusDto result;
            var updates = state != newState;
            if (updates)
            {
                await WriteState(newState);
                result = await GetStatus();
                await NotifyObservers(result);
            }
            else
            {
                // NOTE: Small optimisation to not call GetStatus twice (because it's involves some payload)
                result = await GetStatus();
            }

            await UpdateTimeoutReminder(newState);
            return result;
        }

        private async Task UpdateTimeoutReminder(GameState newState)
        {
            if (newState.Status.IsEndStatus())
            {
                var reminder = await GetReminder(TimeoutCheckReminderName);
                await UnregisterReminder(reminder);
            }
            else
            {
                await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);
            }
        }

        private GameState GetState() => _gameStateHolder.State;

        private async Task WriteState(GameState newState)
        {
            _gameStateHolder.State = newState;
            await _gameStateHolder.WriteStateAsync();
        }

        private async Task NotifyObservers(GameStatusDto update)
        {
            var streamProvider = GetStreamProvider(Constants.GameUpdatesStreamProviderName);
            var grainId = _grainIdProvider.GetGrainId(this);
            var stream = streamProvider.GetStream<GameStatusDto>(grainId, Constants.GameUpdatesStreamName);
            await stream.OnNextAsync(update);
        }

        private static PlayerParticipation GetParticipation(GameState state, AuthorizationToken player)
        {
            if (player == state.XPlayer)
            {
                return PlayerParticipation.X;
            }
            if (player == state.OPlayer)
            {
                return PlayerParticipation.O;
            }
            throw new ArgumentException("Player is not a participant of this game");
        }

        public async Task<GameStatusDto> GetStatus()
        {
            var state = GetState();
            var lobby = GrainFactory.GetGrain<IGameLobby>(Guid.Empty);
            var userNames = await lobby.ResolveUserNamesAsync(state.XPlayer, state.OPlayer);
            var gameMapDto = new GameMapDto(state.Map.DataSnapshot());
            var result = new GameStatusDto(state.Status, gameMapDto, userNames[0], userNames[1]);
            return result;
        }

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case TimeoutCheckReminderName:
                    await ProcessAction(TimeOutAction.Instance);
                    break;
            }
        }

        #region Overrides required for UnitTests
        public new virtual Task<IGrainReminder> GetReminder(string reminderName) => base.GetReminder(reminderName);

        public new virtual Task UnregisterReminder(IGrainReminder reminder) => base.UnregisterReminder(reminder);

        public new virtual Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
            => base.RegisterOrUpdateReminder(reminderName, dueTime, period);

        public new virtual IGrainFactory GrainFactory => base.GrainFactory;

        public new virtual IStreamProvider GetStreamProvider(string name) => base.GetStreamProvider(name);
        #endregion
    }
}
