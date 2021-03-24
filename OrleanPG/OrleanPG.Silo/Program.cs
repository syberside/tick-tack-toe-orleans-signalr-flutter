using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OrleanPG.Grains.Interfaces;
using OrleanPG.Grains.GameLobbyGrain;

namespace OrleanPG.Silo
{
    class Program
    {
        public static int Main(string[] args)
        {
            return MainAsync().Result;
        }

        private static async Task<int> MainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("\n\n Press Enter to terminate...\n\n");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .AddMemoryGrainStorage("game_states_store")
                .AddMemoryGrainStorage("user_states_store")
                .AddMemoryGrainStorage("game_state_store")
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .ConfigureApplicationParts(parts => parts
                    .AddApplicationPart(typeof(GameLobby).Assembly).WithReferences());


            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
