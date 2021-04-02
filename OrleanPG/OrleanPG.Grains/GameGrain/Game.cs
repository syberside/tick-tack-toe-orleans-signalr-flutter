﻿using OrleanPG.Grains.Infrastructure;
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
        private readonly ISubscriptionManager<IGameObserver> _gameObservers;

        public const string TimeoutCheckReminderName = "timeout_check";
        public static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMinutes(1);
        public const int GameSize = GameMap.GameSize;
        private const int _maxIndex = GameSize - 1;


        public Game(
            [PersistentState("game_game_state", "game_state_store")] IPersistentState<GameStorageData> gameState,
            ISubscriptionManager<IGameObserver> gameObservers)
        {
            _gameState = gameState;
            _gameObservers = gameObservers;
        }

        private async Task UpdateState(GameStorageData data)
        {
            _gameState.State = data;
            await _gameState.WriteStateAsync();
            await NotifyObservers();
        }

        private async Task NotifyObservers()
        {
            var update = GetGameStatusDtoFromGameState();
            foreach (var subscription in _gameObservers.GetActualSubscribers)
            {
                subscription.GameStateUpdated(update);
            }

            //new implementation (stream based)
            var streamProvider = GetStreamProvider("GameUpdatesStreamProvider");
            var stream = streamProvider.GetStream<GameStatusDto>(this.GetPrimaryKey(), "GameUpdates");
            await stream.OnNextAsync(update);
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
            await UpdateState(_gameState.State with { XPlayer = playerX, OPlayer = playerO });
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
            if (_gameState.State.Map[x, y] != CellStatus.Empty)
            {
                throw new InvalidOperationException($"Cell {{{x};{y}}} already allocated by {(_gameState.State.Map[x, y] == CellStatus.X ? "X" : "O")}");
            }

            CellStatus stepMarker = CellStatus.Empty;
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
                    stepMarker = CellStatus.X;
                    break;
                case GameState.OTurn:
                    if (player != _gameState.State.OPlayer)
                    {
                        throw new InvalidOperationException();
                    }
                    stepMarker = CellStatus.O;
                    break;
                case GameState.TimedOut:
                    throw new InvalidOperationException();
                default:
                    throw new NotSupportedException();
            }


            var map = _gameState.State.Map.Clone();
            map[x, y] = stepMarker;
            var status = GetNewStatus(stepMarker, x, y, map);

            await UpdateState(_gameState.State with
            {
                Status = status,
                Map = map,
            });
            await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);

            return GetGameStatusDtoFromGameState();
        }

        private GameState GetNewStatus(CellStatus status, int x, int y, GameMap gameMap)
        {
            if (status != CellStatus.O && status != CellStatus.X)
            {
                throw new ArgumentException();
            }
            //check row
            for (var i = 0; i < GameSize; i++)
            {
                if (gameMap[i, y] != status)
                {
                    break;
                }
                if (i == GameSize - 1)
                {
                    return StepToWinState(status);

                }
            }

            //check col
            for (var i = 0; i < GameSize; i++)
            {
                if (gameMap[x, i] != status)
                {
                    break;
                }
                if (i == GameSize - 1)
                {
                    return StepToWinState(status);
                }
            }

            //check diagonal 1
            if (x == y)
            {
                for (var i = 0; i < GameSize; i++)
                {
                    if (gameMap[i, i] != status)
                    {
                        break;
                    }
                    if (i == GameSize - 1)
                    {
                        return StepToWinState(status);
                    }
                }
            }

            //check diagonal 2
            if (x + y == GameSize - 1)
            {

                for (var i = 0; i < GameSize; i++)
                {
                    if (gameMap[GameSize - i - 1, i] != status)
                    {
                        break;
                    }
                    if (i == GameSize - 1)
                    {
                        return StepToWinState(status);

                    }
                }
            }

            return StepToNewStep(status);
        }

        private static GameState StepToNewStep(CellStatus status) => status == CellStatus.X ? GameState.OTurn : GameState.XTurn;

        private static GameState StepToWinState(CellStatus status) => status == CellStatus.X ? GameState.XWin : GameState.OWin;

        public Task<GameStatusDto> GetStatus() => Task.FromResult(GetGameStatusDtoFromGameState());

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
                    await UpdateState(_gameState.State with { Status = GameState.TimedOut });
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

        public Task<GameStatusDto> SubscribeAndMarkAlive(IGameObserver observer)
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
    }
}
