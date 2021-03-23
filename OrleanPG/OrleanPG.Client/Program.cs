using Microsoft.Extensions.Logging;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrleanPG.Client
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            using (var client = await ConnectClient())
            {
                await DoClientWork(client);
                Console.ReadKey();
            }

            return 0;
        }

        private static async Task<IClusterClient> ConnectClient()
        {
            Console.WriteLine("Press enter to connect");
            Console.ReadLine();

            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect();
            Console.WriteLine("Client successfully connected to silo host \n");
            return client;
        }

        private static async Task DoClientWork(IClusterClient clusterClient)
        {
            var lobby = clusterClient.GetGrain<IGameLobby>(Guid.Empty);
            await ListGamesAsync(lobby);

            var token1 = await AuthorizeAsync(lobby, "1");
            var token2 = await AuthorizeAsync(lobby, "2");

            var gameId = await CreateGameAsync(lobby, token1, true);
            await ListGamesAsync(lobby);

            gameId = await CreateGameAsync(lobby, token2, false);
            await ListGamesAsync(lobby);

            await EnterGameAsync(lobby, gameId, token1);

            await ListGamesAsync(lobby);

            await PlayAsync(clusterClient, token1, token2, gameId);
        }

        private static async Task PlayAsync(IClusterClient clusterClient, AuthorizationToken gameTokenFor1, AuthorizationToken gameTokenFor2, GameId id)
        {
            var game = clusterClient.GetGrain<IGame>(id.Value);
            var status = GameStatuses.XTurn;
            while (true)
            {
                AuthorizationToken token;
                switch (status)
                {
                    case GameStatuses.XTurn:
                        token = gameTokenFor1;
                        Console.WriteLine("XTurn");
                        break;
                    case GameStatuses.OTurn:
                        token = gameTokenFor2;
                        Console.WriteLine("OTurn");
                        break;
                    case GameStatuses.XWin:
                        Console.WriteLine("XWin!");
                        return;
                    case GameStatuses.OWin:
                        Console.WriteLine("OWin!");
                        return;
                    default: throw new NotImplementedException();
                }

                var input = Console.ReadLine().Trim().Split(" ").ToArray();
                var x = int.Parse(input[0]);
                var y = int.Parse(input[1]);
                var state = await game.TurnAsync(x, y, token);
                Console.WriteLine(state.GameMap.ToMapString(" | ", " ", "X", "O"));
                status = state.Status;
            }
        }

        private static async Task<AuthorizationToken> AuthorizeAsync(IGameLobby lobby, string user)
        {
            Console.WriteLine($"Enter user {user} name:");
            var userName = (Console.ReadLine() ?? "").Trim();
            Console.WriteLine();
            return await lobby.AuthorizeAsync(userName);
        }

        private static async Task EnterGameAsync(IGameLobby lobby, GameId gameId, AuthorizationToken userToken)
        {
            await lobby.JoinGameAsync(userToken, gameId);
            Console.WriteLine("Entered game");
            Console.WriteLine("");
        }

        private static async Task<GameId> CreateGameAsync(IGameLobby lobby, AuthorizationToken token, bool isX)
        {
            var result = await lobby.CreateNewAsync(token, isX);
            Console.WriteLine($"Created game: {result}");
            Console.WriteLine();
            return result;
        }

        private static async Task ListGamesAsync(IGameLobby lobby)
        {
            var games = await lobby.FindGamesAsync();
            Console.WriteLine("\t\tLobbies: ");
            foreach (var item in games)
            {
                Console.WriteLine($"Game {item.Id}: {item.XPlayerName} VS {item.OPlayerName}");
            }
            Console.WriteLine();

        }
    }
}
