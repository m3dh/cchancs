namespace ChatChan
{
    using System.Threading.Tasks;
    using ChatChan.BackendJob;
    using ChatChan.Common.Configuration;
    using ChatChan.Middleware;
    using ChatChan.Provider;
    using ChatChan.Provider.Queue;
    using ChatChan.Service;
    using ChatChan.Service.Model;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using (IWebHost webHost = WebHost.CreateDefaultBuilder(args)
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

        public static IServiceCollection RegisterAppDependencies(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            // Configurations (appsettings.json)
            serviceCollection
                .Configure<StorageSection>(configuration.GetSection("Storage"))
                .Configure<LimitationsSection>(configuration.GetSection("Limitations"))

                // Providers
                .AddSingleton<CoreDbProvider>()
                .AddSingleton<IMessageQueue<SendMessageQueueEvent>, SendChatMessageQueueProvider>()

                // Entity services
                .AddSingleton<IImageService, ImageService>()
                .AddSingleton<IAccountService, AccountService>()
                .AddSingleton<ITokenService, TokenService>()
                .AddSingleton<IQueueService, QueueService>();

            return serviceCollection;
        }
    }
}
