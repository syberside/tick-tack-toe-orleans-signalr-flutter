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
        private readonly IPersistentState<GameStorageData> _gameState;
        private readonly IGrainIdProvider _grainIdProvider;

        public const string TimeoutCheckReminderName = "timeout_check";
        public static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMinutes(1);

        private const int _maxIndex = GameMap.GameSize - 1;


        public GameGrain(
            [PersistentState("game_game_state", "game_state_store")] IPersistentState<GameStorageData> gameState,
            IGrainIdProvider grainIdProvider)
        {
            _gameState = gameState;
            _grainIdProvider = grainIdProvider;
        }

        private async Task UpdateState(GameStorageData data)
        {
            _gameState.State = data;
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

            var result = await GetGameStatusDtoFromGameState();
            return result;
        }


        public async Task<GameStatusDto> TurnAsync(int x, int y, AuthorizationToken player)
        {
            if (!_gameState.State.IsInitialized)
            {
                throw new InvalidOperationException();
            }
            if (x > _maxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, $"Should be less than {GameMap.GameSize}");
            }
            if (y > _maxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Should be less than {GameMap.GameSize}");
            }
            if (_gameState.State.Map[x, y] != CellStatus.Empty)
            {
                throw new InvalidOperationException($"Cell {{{x};{y}}} already allocated by {(_gameState.State.Map[x, y] == CellStatus.X ? "X" : "O")}");
            }

            var stepMarker = CellStatus.Empty;
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

            return await GetGameStatusDtoFromGameState();
        }

        private GameState GetNewStatus(CellStatus stepBy, int x, int y, GameMap gameMap)
        {
            if (stepBy != CellStatus.O && stepBy != CellStatus.X)
            {
                throw new ArgumentException();
            }

            //check for draw
            if (!gameMap.HaveEmptyCells)
            {
                return GameState.Draw;
            }

            //check row
            for (var i = 0; i < GameMap.GameSize; i++)
            {
                if (gameMap[i, y] != stepBy)
                {
                    break;
                }

                if (i == _maxIndex)
                {
                    return StepToWinState(stepBy);

                }
            }

            //check col
            for (var i = 0; i < GameMap.GameSize; i++)
            {
                if (gameMap[x, i] != stepBy)
                {
                    break;
                }
                if (i == _maxIndex)
                {
                    return StepToWinState(stepBy);
                }
            }

            //check diagonal 1
            if (x == y)
            {
                for (var i = 0; i < GameMap.GameSize; i++)
                {
                    if (gameMap[i, i] != stepBy)
                    {
                        break;
                    }
                    if (i == _maxIndex)
                    {
                        return StepToWinState(stepBy);
                    }
                }
            }

            //check diagonal 2
            if (x + y == _maxIndex)
            {

                for (var i = 0; i < GameMap.GameSize; i++)
                {
                    if (gameMap[_maxIndex - i, i] != stepBy)
                    {
                        break;
                    }
                    if (i == _maxIndex)
                    {
                        return StepToWinState(stepBy);

                    }
                }
            }

            return StepToNewStep(stepBy);
        }

        private static GameState StepToNewStep(CellStatus status) => status == CellStatus.X ? GameState.OTurn : GameState.XTurn;

        private static GameState StepToWinState(CellStatus status) => status == CellStatus.X ? GameState.XWin : GameState.OWin;

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
            var userNames = await lobby.ResolveUserNamesAsync(_gameState.State.XPlayer, _gameState.State.OPlayer);
            return new GameStatusDto(_gameState.State.Status, _gameState.State.Map, userNames[0], userNames[1]);
        }
    }

    public static class GameHelper
    {

    }
}
