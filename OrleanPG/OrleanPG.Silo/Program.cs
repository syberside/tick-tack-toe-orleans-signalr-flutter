using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OrleanPG.Grains.GameLobbyGrain;
using Orleans.Reminders.AzureStorage;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;

namespace OrleanPG.Silo
{
    class Program
    {
        /// <summary>
        /// TODO: Unsecure, should be moved to configuration out of repository. Token should be revoked and replaced with new one.
        /// </summary>
        private const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=ticktactorstorage;AccountKey=fZ88n7XGOZiAMvvgKJawqQqqaHV47bNNfd3V3WckvJue0HezVu5VPWli4gi0IRWZ3wiMn0li5rIp5ArcmHHdrA==;EndpointSuffix=core.windows.net";

        public static async Task<int> Main(string[] args)
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
                .AddAzureQueueStreams("GameUpdatesStreamProvider", cfg =>
                 {
                     cfg.ConfigureAzureQueue(queueCfg =>
                     {
                         queueCfg.Configure(SetupConnectionString);
                     });
                 })
                .AddMemoryGrainStorage("PubSubStore")
                .UseAzureTableReminderService(SetupConnectionString)
                .AddAzureTableGrainStorage("game_states_store", SetupStore)
                .AddAzureTableGrainStorage("user_states_store", SetupStore)
                .AddAzureTableGrainStorage("game_state_store", SetupStore)
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
                    .AddApplicationPart(typeof(GameLobby).Assembly).WithReferences())
                .ConfigureServices(s => s.AddTransient<ISubscriptionManager<IGameObserver>>((sp) => new SubscriptionManager<IGameObserver>(() => DateTime.Now)));


            var host = builder.Build();
            await host.StartAsync();
            return host;
        }

        private static void SetupConnectionString(AzureQueueOptions options)
        {
            options.ConnectionString = ConnectionString;
        }

        private static void SetupConnectionString(AzureTableReminderStorageOptions options)
        {
            options.ConnectionString = ConnectionString;

        }

        private static void SetupStore(AzureTableStorageOptions options)
        {
            options.UseJson = true;
            options.ConnectionString = ConnectionString;
        }
    }
}
