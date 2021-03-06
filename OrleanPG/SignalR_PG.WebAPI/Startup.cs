using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SignalR_PG.WebAPI.Hubs;
using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Configuration;
using Newtonsoft.Json.Serialization;
using Orleans.Hosting;
using OrleanPG.Grains.Interfaces;
using System.Threading.Tasks;

namespace SignalR_PG.WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services
                .AddSignalR()
                .AddNewtonsoftJsonProtocol();
            services.AddSingleton((sp) => CreateClient());
            services.AddSingleton<IGrainFactory>((sp) => sp.GetRequiredService<IClusterClient>());

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<GamesHub>("/gamesHub");
            });
        }

        public IClusterClient CreateClient()
        {
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .AddAzureQueueStreams(Constants.GameUpdatesStreamProviderName, (ClusterClientAzureQueueStreamConfigurator cfg) => { })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .Build();
            return client;
        }

        public static async Task InitServicesAsync(IHost host)
        {
            var clusterClient = host.Services.GetRequiredService<IClusterClient>();
            await clusterClient.Connect();
        }
    }
}
