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
        private readonly IPersistentState<GameState> _gameState;
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
            _gameState = gameState;
            _grainIdProvider = grainIdProvider;
            _gameEngine = gameEngine;
        }

        private GameState State => _gameState.State;

        private async Task UpdateStateAndNotifyObserversIfChanged(GameState newState)
        {
            if (State == newState)
            {
                return;
            }
            _gameState.State = newState;
            await _gameState.WriteStateAsync();
            await NotifyObservers();
        }

        private async Task NotifyObservers()
        {
            var update = await GetGameStatusDtoFromGameState();
            var streamProvider = GetStreamProvider(Constants.GameUpdatesStreamProviderName);
            var grainId = _grainIdProvider.GetGrainId(this);
            var stream = streamProvider.GetStream<GameStatusDto>(grainId, Constants.GameUpdatesStreamName);
            await stream.OnNextAsync(update);
        }

        public async Task<GameStatusDto> StartAsync(AuthorizationToken playerX, AuthorizationToken playerO)
        {
            var action = new InitializeAction(playerX, playerO);
            var newState = _gameEngine.Process(action, State);

            await UpdateStateAndNotifyObserversIfChanged(newState);

            await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);

            return await GetGameStatusDtoFromGameState();
        }


        public async Task<GameStatusDto> TurnAsync(GameMapPoint position, AuthorizationToken player)
        {
            var turn = new UserTurnAction(position, GetParticipation(player));
            var newState = _gameEngine.Process(turn, State);

            await UpdateStateAndNotifyObserversIfChanged(newState);

            await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);

            return await GetGameStatusDtoFromGameState();
        }

        private PlayerParticipation GetParticipation(AuthorizationToken player)
        {
            if (player == State.XPlayer)
            {
                return PlayerParticipation.X;
            }
            if (player == State.OPlayer)
            {
                return PlayerParticipation.O;
            }
            throw new ArgumentException("Player is not a participant of this game");
        }

        public Task<GameStatusDto> GetStatus() => GetGameStatusDtoFromGameState();

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case TimeoutCheckReminderName:
                    await CheckTimeout();
                    break;
            }
        }

        private async Task CheckTimeout()
        {
            var newState = _gameEngine.Process(TimeOutAction.Instance, State);

            await UpdateStateAndNotifyObserversIfChanged(newState);

            var reminder = await GetReminder(TimeoutCheckReminderName);
            await UnregisterReminder(reminder);
        }

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public new virtual Task<IGrainReminder> GetReminder(string reminderName) => base.GetReminder(reminderName);

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public new virtual Task UnregisterReminder(IGrainReminder reminder) => base.UnregisterReminder(reminder);

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public new virtual Task<IGrainReminder> RegisterOrUpdateReminder(string reminderName, TimeSpan dueTime, TimeSpan period)
            => base.RegisterOrUpdateReminder(reminderName, dueTime, period);

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public new virtual IGrainFactory GrainFactory => base.GrainFactory;

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public new virtual IStreamProvider GetStreamProvider(string name) => base.GetStreamProvider(name);

        private async Task<GameStatusDto> GetGameStatusDtoFromGameState()
        {
            var lobby = GrainFactory.GetGrain<IGameLobby>(Guid.Empty);
            var userNames = await lobby.ResolveUserNamesAsync(State.XPlayer, State.OPlayer);
            var gameMapDto = new GameMapDto(State.Map.DataSnapshot());
            var result = new GameStatusDto(State.Status, gameMapDto, userNames[0], userNames[1]);
            return result;
        }
    }
}
