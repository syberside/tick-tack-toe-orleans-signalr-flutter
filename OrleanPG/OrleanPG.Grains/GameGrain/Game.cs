using Microsoft.Extensions.Logging;
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

            return new GameStatusDto(status, map);
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
    }
}
