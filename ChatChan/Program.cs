namespace ChatChan
{
    using System.IO;
    using System.Threading.Tasks;

    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Provider.Partition;
    using ChatChan.Provider.Queue;
    using ChatChan.Service;

    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using (IWebHost webHost = GetBuilder()
                .UseUrls("http://*:8080")
                .UseStartup<HttpService>()
                .Build())
            {
                // Start the Kestrel server for the chan service.
                await webHost.StartAsync();

                // Host the backend processor thread.
                await new JobProcessor().DoWork();
            }
        }

        public static IWebHostBuilder GetBuilder()
        {
            IWebHostBuilder builder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UseIISIntegration()
                .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                });

            return builder;
        }

        public static IServiceCollection RegisterAppDependencies(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            // Configurations (appsettings.json)
            serviceCollection
                .Configure<StorageSection>(configuration.GetSection("Storage"))
                .Configure<LimitationsSection>(configuration.GetSection("Limitations"))

                // Providers
                .AddSingleton<CoreDbProvider>()
                .AddSingleton<IDataPartitionProvider, DataPartitionsManager>()
                .AddSingleton<IMessageQueue, MessageQueueProvider>()

                // Entity services
                .AddSingleton<IImageService, ImageService>()
                .AddSingleton<IAccountService, AccountService>()
                .AddSingleton<ITokenService, TokenService>()
                .AddSingleton<IChannelService, ChannelService>()
                .AddSingleton<IMessageService, MessageService>()
                .AddSingleton<IParticipantService, ParticipantService>()
                .AddSingleton<IQueueService, QueueService>();

            return serviceCollection;
        }
    }
}
