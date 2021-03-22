using Microsoft.Extensions.Logging;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrleanPG.Client
{
    class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                using (var client = await ConnectClient())
                {
                    await DoClientWork(client);
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nException while trying to run client: {e.Message}");
                Console.WriteLine("Make sure the silo the client is trying to connect to is running.");
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
                return 1;
            }
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

        private static async Task DoClientWork(IClusterClient client)
        {
            var game = client.GetGrain<IGameLobby>(0);
            while (true)
            {
                Console.WriteLine("Enter command: ");
                var command = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
                switch (command)
                {
                    default:
                    case "": continue;
                    case "q": break;
                    case "l":
                        await ListGamesAsync(game);
                        continue;
                    case "c":
                        await CreateGameAsync(game);
                        continue;
                    case "e":
                        await EnterGameAsync(game, client);
                        continue;
                    case "a1":
                        await Authorize1Async(game);
                        continue;
                    case "a2":
                        await Authorize2Async(game);
                        continue;
                }
            }
        }

        private static AuthorizationToken _token2;

        private static async Task Authorize2Async(IGameLobby game)
        {
            Console.WriteLine("Enter user 2 name:");
            var user2Name = (Console.ReadLine() ?? "").Trim();
            _token2 = await game.AuthorizeAsync(user2Name);
        }

        private static AuthorizationToken _token1;

        private static async Task Authorize1Async(IGameLobby game)
        {
            Console.WriteLine("Enter user 1 name:");
            var user1Name = (Console.ReadLine() ?? "").Trim();
            _token1 = await game.AuthorizeAsync(user1Name);
        }

        private static async Task EnterGameAsync(IGameLobby game, IClusterClient cluster)
        {
            if (_token2 == null)
            {
                Console.WriteLine("Enter user 2 name first");
            }
            Console.WriteLine("Enter game id:");
            var gameId = Guid.Parse(Console.ReadLine().Trim());
            var token = await game.JoinGameAsync(_token2, new GameId(gameId));

            var gameInitilizer = cluster.GetGrain<IGameInitializer>(token.Value);
            await gameInitilizer.StartAsync(_token1, _token2);
            var gameClient = cluster.GetGrain<IGame>(token.Value);
            await gameClient.TurnAsync(0, 0, _token1);
        }

        private static async Task CreateGameAsync(IGameLobby game)
        {
            if (_token1 == null)
            {
                Console.WriteLine("Enter user 1 name first");
            }
            var token = await game.CreateNewAsync(_token1, new Random().Next(2) > 1);
            //TODO: use token, how user 1 will now id of created game?
        }

        private Dictionary<GameId, GameToken> _user1Tokens = new Dictionary<GameId, GameToken>();

        private static async Task ListGamesAsync(IGameLobby game)
        {
            var games = await game.FindGamesAsync();
            foreach (var item in games)
            {
                Console.WriteLine($"Game {item.Id}: {item.XPlayerName} VS {item.OPlayerName}");
            }
        }
    }
}
