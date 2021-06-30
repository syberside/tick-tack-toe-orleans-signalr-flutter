using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace OrleanPG.Grains.GameBot
{
    [ImplicitStreamSubscription(Constants.GameUpdatesStreamName)]
    public class GameBotGrain : Grain, IGameBot
    {
        private readonly IGrainIdProvider _grainIdProvider;
        private readonly IPersistentState<GameBotStorageData> _botData;
        private readonly Random _random;
        private StreamSubscriptionHandle<GameStatusDto>? _subscriptionHandlle;

        public GameBotGrain(
            [PersistentState("game_bot_state", "game_bot_state_store")] IPersistentState<GameBotStorageData> botData,
            IGrainIdProvider grainIdProvider,
            Random random)
        {
            _grainIdProvider = grainIdProvider;
            _botData = botData;
            _random = random;
        }

        public override async Task OnActivateAsync()
        {
            var grainId = _grainIdProvider.GetGrainId(this);
            var streamProvider = GetStreamProvider(Constants.GameUpdatesStreamProviderName);
            var stream = streamProvider.GetStream<GameStatusDto>(grainId, Constants.GameUpdatesStreamName);
            _subscriptionHandlle = await stream.SubscribeAsync(OnGameUpdated);
        }

        internal async Task OnGameUpdated(GameStatusDto update, StreamSequenceToken token)
        {
            if (_botData.State.Token == null)
            {
                // Bot activation occures before joining game
                return;
            }
            var grainId = _grainIdProvider.GetGrainId(this);

            if (update.Status.IsEndStatus())
            {
                await CleanupAsync();
            }

            var takeStepForO = update.Status == GameStatus.OTurn && !_botData.State.PlayForX;
            var takeStepForX = update.Status == GameStatus.XTurn && _botData.State.PlayForX;
            var takeStep = takeStepForO || takeStepForX;
            if (!takeStep)
            {
                return;
            }

            (int x, int y) = GetNextTurn(update);
            var authToken = _botData.State.Token;

            var game = GrainFactory.GetGrain<IGame>(grainId);
            await game.TurnAsync(x, y, authToken);
        }

        private async Task CleanupAsync()
        {
            if (_subscriptionHandlle != null)
            {
                await _subscriptionHandlle.UnsubscribeAsync();
            }
            await _botData.ClearStateAsync();
        }

        private (int x, int y) GetNextTurn(GameStatusDto update)
        {
            var availableCells = update.GameMap.GetAvailableCells();
            var randomPosition = _random.Next(availableCells.Length);
            return availableCells[randomPosition];
        }

        public async Task InitAsync(AuthorizationToken token, bool playForX)
        {
            if (token == null)
            {
                throw new ArgumentNullException();
            }
            await WriteStateUpdate(new GameBotStorageData(token, playForX));
        }

        private async Task WriteStateUpdate(GameBotStorageData update)
        {
            _botData.State = update;
            await _botData.WriteStateAsync();
        }

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public virtual new IStreamProvider GetStreamProvider(string name) => base.GetStreamProvider(name);

        /// <summary>
        /// NOTE: Required for unit tests
        /// </summary>
        public virtual new IGrainFactory GrainFactory => base.GrainFactory;

    }
}
