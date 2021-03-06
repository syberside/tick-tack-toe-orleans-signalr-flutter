using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrleanPG.Grains.GameLobbyGrain;
using OrleanPG.Grains.Infrastructure;
using OrleanPG.Grains.Interfaces;
using OrleanPG.Silo.IoC;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Reminders.AzureStorage;
using System;
using System.Threading.Tasks;

namespace OrleanPG.Silo
{
    class Program
    {
        private static string? ConnectionString;
        public static async Task<int> Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true);
            var config = builder.Build();
            ConnectionString = config.GetConnectionString("AzureStorage");
            var host = await StartSilo();
            Console.WriteLine("\n\n Press Enter to terminate...\n\n");
            Console.ReadLine();

            await host.StopAsync();

            return 0;
        }


        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .AddAzureQueueStreams(Constants.GameUpdatesStreamProviderName, cfg =>
                 {
                     cfg.ConfigureAzureQueue(queueCfg =>
                     {
                         queueCfg.Configure(SetupConnectionString);
                     });
                 })
                //PubSubStore is required for Queue streaming
                .AddMemoryGrainStorage("PubSubStore")
                .UseAzureTableReminderService(SetupConnectionString)
                .AddAzureTableGrainStorage("game_bot_state_store", SetupStore)
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
                    .AddApplicationPart(typeof(GameLobbyGrain).Assembly).WithReferences())
                .ConfigureServices(services => services
                    .AddSingleton<IGrainIdProvider, GrainIdProvider>()
                    .AddSingleton((sp) => new Random(DateTime.Now.Millisecond))
                    .AddGameEngine()
                    ); ;

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
