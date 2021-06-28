using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OrleanPG.Grains.Game
{
    public class GameGrain : Grain, IGame, IGameInitializer
    {
        private readonly IPersistentState<GameStorageData> _gameState;
        private readonly IGrainIdProvider _grainIdProvider;

        public const string TimeoutCheckReminderName = "timeout_check";
        public static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMinutes(1);

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

        private async Task UpdateStateIfChanged(GameEngineState oldState, GameEngineState newState)
        {
            if (oldState == newState)
            {
                return;
            }
            await UpdateState(_gameState.State with { Map = newState.Map, Status = newState.GameState });
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

            var engine = BuildEngine();
            var engineState = BuildEngineState();
            var turn = new UserTurn(x, y, GetParticipation(player));
            var newState = engine.Process(turn, engineState);

            await UpdateStateIfChanged(engineState, newState);

            await RegisterOrUpdateReminder(TimeoutCheckReminderName, TimeoutPeriod, TimeoutPeriod);

            return await GetGameStatusDtoFromGameState();
        }

        private GameEngineState BuildEngineState() => new GameEngineState(_gameState.State.Map, _gameState.State.Status);

        private static GameEngine BuildEngine()
        {
            // TODO: use ioc
            return new GameEngine(new IWinChecker[]
            {
                new ByColWinChecker(),
                new ByRowWinChecker(),
                new ByMainDiagonalWinChecker(),
                new BySideDiagonalWinChecker(),
            });
        }

        private PlayerParticipation GetParticipation(AuthorizationToken player)
        {
            if (player == _gameState.State.XPlayer)
            {
                return PlayerParticipation.X;
            }
            if (player == _gameState.State.OPlayer)
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
            var engineState = BuildEngineState();
            var engine = BuildEngine();
            var newState = engine.Process(new TimeOut(), engineState);

            await UpdateStateIfChanged(engineState, newState);

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

    public class GameEngine
    {
        private readonly IWinChecker[] _winCheckers;

        public GameEngine(IWinChecker[] winCheckers)
        {
            _winCheckers = winCheckers;
        }

        public GameEngineState Process(GameAction action, GameEngineState state)
        {
            return Process((dynamic)action, state);
        }

        /// <summary>
        /// NOTE: Default callback for multuple dispatch
        /// </summary>
        private GameEngineState Process(object action, GameEngineState _)
            => throw new NotSupportedException($"Action {action?.GetType()} is not supported");

        private GameEngineState Process(UserTurn action, GameEngineState state)
        {
            var x = action.X;
            var y = action.Y;
            var map = state.Map;
            if (map.IsCellBusy(x, y))
            {
                throw new InvalidOperationException($"Cell {{{x};{y}}} already allocated by {(map[x, y] == CellStatus.X ? "X" : "O")}");
            }

            var gameState = state.GameState;
            if (gameState.IsEndStatus())
            {
                throw new InvalidOperationException($"Game is in end status: {gameState}");
            }
            var (expectedNextPlayer, stepMarker) = PlayerParticiptionExtensions.PlayerForState(gameState);
            if (expectedNextPlayer != action.StepBy)
            {
                throw new InvalidOperationException();
            }

            var updatedMap = UpdateMap(x, y, map, stepMarker);
            var status = GetNewStatus(action.StepBy, updatedMap);

            return new GameEngineState(updatedMap, status);
        }

        private GameEngineState Process(TimeOut _, GameEngineState engineState)
        {
            if (engineState.GameState.IsEndStatus())
            {
                return engineState;
            }
            return engineState with { GameState = GameState.TimedOut };
        }

        private static GameMap UpdateMap(int x, int y, GameMap map, CellStatus stepMarker)
        {
            var updatedMap = map.Clone();
            updatedMap[x, y] = stepMarker;
            return updatedMap;
        }

        private GameState GetNewStatus(PlayerParticipation stepBy, GameMap map)
        {
            if (!map.HaveEmptyCells)
            {
                return GameState.Draw;
            }
            var win = _winCheckers
              .Select(x => x.CheckIfWin(map, stepBy))
              .Where(x => x != null)
              .FirstOrDefault();
            // NOTE: Win content is currently not used, but will be used for drawing a game result in UI in the future
            if (win == null)
            {
                return StepToNewStep(stepBy);
            }
            else
            {
                return StepToWinState(stepBy);
            }
        }
        private static GameState StepToNewStep(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameState.OTurn : GameState.XTurn;

        private static GameState StepToWinState(PlayerParticipation p)
            => p == PlayerParticipation.X ? GameState.XWin : GameState.OWin;
    }

    public class GameAction { }

    public class TimeOut : GameAction { }

    public class UserTurn : GameAction
    {
        public int X { get; }

        public int Y { get; }

        public PlayerParticipation StepBy { get; }

        public UserTurn(int x, int y, PlayerParticipation participation)
        {
            if (x > GameMap.MaxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(x), x, $"Should be less than {GameMap.GameSize}");
            }
            if (y > GameMap.MaxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(y), y, $"Should be less than {GameMap.GameSize}");
            }

            X = x;
            Y = y;
            StepBy = participation;
        }
    }

    public enum PlayerParticipation
    {
        X,
        O
    }

    public class Win
    {
        public int Index { get; }

        public GameAxis Axis { get; }

        public Win(int index, GameAxis axis)
        {
            Index = index;
            Axis = axis;
        }
    }

    public interface IWinChecker
    {
        Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer);
    }

    public class ByRowWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => Enumerable.Range(0, GameMap.GameSize)
            .Where(x => map.IsRowFilledBy(x, forPlayer.ToCellStatus()))
            .Select(x => new Win(x, GameAxis.X)).FirstOrDefault();
    }

    public class ByColWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => Enumerable.Range(0, GameMap.GameSize)
            .Where(y => map.IsColFilledBy(y, forPlayer.ToCellStatus()))
            .Select(y => new Win(y, GameAxis.Y)).FirstOrDefault();
    }

    public class ByMainDiagonalWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => map.IsMainDiagonalFilledBy(forPlayer.ToCellStatus()) ? new Win(0, GameAxis.MainDiagonal) : null;
    }

    public class BySideDiagonalWinChecker : IWinChecker
    {
        public Win? CheckIfWin(GameMap map, PlayerParticipation forPlayer)
            => map.IsSideDiagonalFilledBy(forPlayer.ToCellStatus()) ? new Win(0, GameAxis.SideDiagonal) : null;
    }

    public static class PlayerParticiptionExtensions
    {
        public static CellStatus ToCellStatus(this PlayerParticipation participaton)
        {
            switch (participaton)
            {
                case PlayerParticipation.X: return CellStatus.X;
                case PlayerParticipation.O: return CellStatus.O;
                default: throw new NotSupportedException();
            }
        }

        public static (PlayerParticipation, CellStatus marker) PlayerForState(GameState status)
        {
            switch (status)
            {
                case GameState.XTurn: return (PlayerParticipation.X, CellStatus.X);
                case GameState.OTurn: return (PlayerParticipation.O, CellStatus.O);
                default: throw new NotSupportedException();
            }
        }
    }

    public record GameEngineState(GameMap Map, GameState GameState);
}
