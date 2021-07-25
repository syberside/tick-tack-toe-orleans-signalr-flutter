using OrleanPG.Grains.Game.Engine;
using OrleanPG.Grains.Game.Engine.Actions;
using OrleanPG.Grains.Game.GrainLogic;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Game
{

    public class GameGrain : Grain, IGame, IGameInitializer
    {
        private readonly IPersistentState<GameState> _gameStateHolder;
        private readonly IGameEngine _gameEngine;
        private GameGrainLogic? _processer;

        private const string TimeoutCheckReminderName = "timeout_check";
        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMinutes(1);

        public GameGrain(
            [PersistentState("game_game_state", "game_state_store")]
            IPersistentState<GameState> gameState,
            IGameEngine gameEngine)
        {
            _gameStateHolder = gameState;
            _gameEngine = gameEngine;
        }

        public override async Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(Constants.GameUpdatesStreamProviderName);
            var grainId = this.GetPrimaryKey();
            var stream = streamProvider.GetStream<GameStatusDto>(grainId, Constants.GameUpdatesStreamName);
            var reminderHandle = new GrainReminderToReminderHandleAdaptor(
                () => RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod),
                async () =>
                {
                    var reminder = await GetReminder(TimeoutCheckReminderName);
                    await UnregisterReminder(reminder);
                }
                );
            var gameStateStore = new DelegatesToGameStateStoreAdapter(
                () => _gameStateHolder.State,
                async (state) =>
                {
                    _gameStateHolder.State = state;
                    await _gameStateHolder.WriteStateAsync();
                });
            var gameLobby = GrainFactory.GetGrain<IGameLobby>(Guid.Empty);

            _processer = new GameGrainLogic(
                gameStateStore,
                _gameEngine,
                gameLobby,
                stream,
                reminderHandle
                );
            await base.OnActivateAsync();
        }


        public Task<GameStatusDto> StartAsync(AuthorizationToken playerX, AuthorizationToken playerO)
            => ProcessAction(new InitializeAction(playerX, playerO));

        public Task<GameStatusDto> TurnAsync(GameMapPoint position, AuthorizationToken player)
            => ProcessAction(new UserTurnAction(position, GetParticipation(_processer!.GetState(), player)));

        private async Task<GameStatusDto> ProcessAction(IGameAction action)
        {
            await _processer!.ProcessAction(action);
            return await _processer.GetStatus();
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

        public Task<GameStatusDto> GetStatus() => _processer!.GetStatus();

        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            switch (reminderName)
            {
                case TimeoutCheckReminderName:
                    await _processer!.ProcessAction(TimeOutAction.Instance);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown reminder: {reminderName}");
            }
        }
    }
}

