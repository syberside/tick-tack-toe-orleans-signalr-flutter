using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains.GameGrain
{
    public class Game : Grain, IGame, IGameInitializer
    {
        private readonly IPersistentState<GameStorageData> _gameState;
        /// <summary>
        /// TODO: Inject and cover with tests related code
        /// </summary>
        private readonly SubscriptionManager<IGameObserver> _gameObservers = new SubscriptionManager<IGameObserver>(() => DateTime.Now);

        public const string TimeoutCheckReminderName = "timeout_check";
        public static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMinutes(1);
        public const int GameSize = GameMap.GameSize;
        private const int _maxIndex = GameSize - 1;


        public Game(
            [PersistentState("game_game_state", "game_state_store")] IPersistentState<GameStorageData> gameState
            )
        {
            _gameState = gameState;
        }

        public async Task StartAsync(AuthorizationToken playerX, AuthorizationToken playerO)
        {
            if (playerX == null)
            {
                throw new ArgumentNullException();
            }
            if (playerO == null)
            {
                throw new ArgumentNullException();
            }
            if (playerO == playerX)
            {
                throw new ArgumentException();
            }
            if (_gameState.State.IsInitialized)
            {
                throw new InvalidOperationException();
            }
            _gameState.State = _gameState.State with { XPlayer = playerX, OPlayer = playerO };
            await _gameState.WriteStateAsync();
            await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);
        }


        public async Task<GameStatusDto> TurnAsync(int x, int y, AuthorizationToken player)
        {
            if (!_gameState.State.IsInitialized)
            {
                throw new InvalidOperationException();
            }
            if (x > _maxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, $"Should be less than {GameSize}");
            }
            if (y > _maxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Should be less than {GameSize}");
            }
            if (_gameState.State.Map[x, y] != null)
            {
                throw new InvalidOperationException($"Cell {{{x};{y}}} already allocated by {(_gameState.State.Map[x, y] == true ? "X" : "Y")}");
            }

            bool stepMarker;
            switch (_gameState.State.Status)
            {
                case GameState.OWin:
                case GameState.XWin:
                    throw new InvalidOperationException();
                case GameState.XTurn:
                    if (player != _gameState.State.XPlayer)
                    {
                        throw new InvalidOperationException();
                    }
                    stepMarker = true;
                    break;
                case GameState.OTurn:
                    if (player != _gameState.State.OPlayer)
                    {
                        throw new InvalidOperationException();
                    }
                    stepMarker = false;
                    break;
                case GameState.TimedOut:
                    throw new InvalidOperationException();
                default:
                    throw new NotSupportedException();
            }


            var map = _gameState.State.Map.Clone();
            map[x, y] = stepMarker;
            var status = GetNewStatus(stepMarker, x, y, map);

            _gameState.State = _gameState.State with
            {
                Status = status,
                Map = map,
            };
            await _gameState.WriteStateAsync();
            await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);
            NotifyObservers();

            return GetGameStatusDtoFromGameState();
        }

        private GameState GetNewStatus(bool stepMarker, int x, int y, GameMap gameMap)
        {
            //check row
            for (var i = 0; i < GameSize; i++)
            {
                if (gameMap[i, y] != stepMarker)
                {
                    break;
                }
                if (i == GameSize - 1)
                {
                    return stepMarker ? GameState.XWin : GameState.OWin;

                }
            }

            //check col
            for (var i = 0; i < GameSize; i++)
            {
                if (gameMap[x, i] != stepMarker)
                {
                    break;
                }
                if (i == GameSize - 1)
                {
                    return stepMarker ? GameState.XWin : GameState.OWin;

                }
            }

            //check diagonal 1
            if (x == y)
            {
                for (var i = 0; i < GameSize; i++)
                {
                    if (gameMap[i, i] != stepMarker)
                    {
                        break;
                    }
                    if (i == GameSize - 1)
                    {
                        return stepMarker ? GameState.XWin : GameState.OWin;

                    }
                }
            }

            //check diagonal 2
            if (x + y == GameSize - 1)
            {

                for (var i = 0; i < GameSize; i++)
                {
                    if (gameMap[GameSize - i - 1, i] != stepMarker)
                    {
                        break;
                    }
                    if (i == GameSize - 1)
                    {
                        return stepMarker ? GameState.XWin : GameState.OWin;

                    }
                }
            }

            return stepMarker ? GameState.OTurn : GameState.XTurn;
        }

        public Task<GameMap> GetMapAsync() => Task.FromResult(_gameState.State.Map);

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
            switch (_gameState.State.Status)
            {
                case GameState.XWin:
                case GameState.OWin:
                case GameState.TimedOut:
                    break;
                case GameState.OTurn:
                case GameState.XTurn:
                    _gameState.State = _gameState.State with { Status = GameState.TimedOut };
                    await _gameState.WriteStateAsync();
                    break;
                default:
                    throw new NotImplementedException();
            }
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

        public Task<GameStatusDto> SubscribeToUpdatesOrMarkAlive(IGameObserver observer)
        {
            _gameObservers.SubscribeOrMarkAlive(observer);
            return Task.FromResult(GetGameStatusDtoFromGameState());
        }

        private GameStatusDto GetGameStatusDtoFromGameState() => new GameStatusDto(_gameState.State.Status, _gameState.State.Map);

        public Task UnsubscribeFromUpdates(IGameObserver observer)
        {
            _gameObservers.Unsubscribe(observer);
            return Task.CompletedTask;
        }

        private void NotifyObservers()
        {
            foreach (var subscription in _gameObservers.GetActualSubscribers)
            {
                subscription.GameStateUpdated(GetGameStatusDtoFromGameState());
            }
        }
    }
}
