using Microsoft.Extensions.Logging;
using OrleanPG.Grains.Interfaces;
using Orleans;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Grains
{
    public class Game : Grain, IGame, IGameInitializer
    {
        private readonly ILogger<Game> _logger;
        private GameStatus _status;
        public AuthorizationToken? XPlayer { get; private set; }
        public AuthorizationToken? OPlayer { get; private set; }
        public const int GameSize = 3;
        private const int _maxIndex = GameSize - 1;


        public Game(ILogger<Game> logger)
        {
            _logger = logger;
            _status = new GameStatus(GameStatuses.XTurn, new bool?[GameSize, GameSize]);
        }

        public Task StartAsync(AuthorizationToken playerX, AuthorizationToken playerO)
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
            if (IsInitialized)
            {
                throw new InvalidOperationException();
            }
            XPlayer = playerX;
            OPlayer = playerO;
            return Task.CompletedTask;
        }

        public bool IsInitialized => XPlayer != null && OPlayer != null;

        public Task<GameStatus> TurnAsync(int x, int y, AuthorizationToken player)
        {
            if (!IsInitialized)
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
            if (_status.GameMap[x, y] != null)
            {
                throw new InvalidOperationException($"Cell {{{x};{y}}} already allocated by {(_status.GameMap[x, y] == true ? "X" : "Y")}");
            }

            bool stepMarker;
            switch (_status.Status)
            {
                case GameStatuses.OWin:
                case GameStatuses.XWin:
                    throw new InvalidOperationException();
                case GameStatuses.XTurn:
                    if (player != XPlayer)
                    {
                        throw new InvalidOperationException();
                    }
                    stepMarker = true;
                    break;
                case GameStatuses.OTurn:
                    if (player != OPlayer)
                    {
                        throw new InvalidOperationException();
                    }
                    stepMarker = false;
                    break;
                default:
                    throw new NotSupportedException();

            }


            var map = (bool?[,])_status.GameMap.Clone();
            map[x, y] = stepMarker;
            var status = GetNewStatus(stepMarker, x, y, map);

            _status = _status with { GameMap = map, Status = status };

            return Task.FromResult(_status);
        }

        private GameStatuses GetNewStatus(bool stepMarker, int x, int y, bool?[,] gameMap)
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
                    return stepMarker ? GameStatuses.XWin : GameStatuses.OWin;

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
                    return stepMarker ? GameStatuses.XWin : GameStatuses.OWin;

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
                        return stepMarker ? GameStatuses.XWin : GameStatuses.OWin;

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
                        return stepMarker ? GameStatuses.XWin : GameStatuses.OWin;

                    }
                }
            }

            return stepMarker ? GameStatuses.OTurn : GameStatuses.XTurn;
        }
    }
}
