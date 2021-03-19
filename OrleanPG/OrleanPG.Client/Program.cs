using Microsoft.Extensions.Logging;
using OrleanPG.Grains.Interfaces;
using Orleans;
using Orleans.Configuration;
using System;
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
            // example of calling grains from the initialized client
            var game = client.GetGrain<ITickTacToeGameHolder>(0);
            while (true)
            {
                Console.WriteLine("Press enter to send a message");
                Console.ReadLine();
                var response = await game.MakeATurn(1, 2, Guid.NewGuid());
                Console.WriteLine($"\n\n{response}\n\n");
            };
        }
    }
}
